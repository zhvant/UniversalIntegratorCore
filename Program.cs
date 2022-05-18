using DryIoc;
using SBAST.UniversalIntegrator.Configs;
using SBAST.UniversalIntegrator.DB;
using SBAST.UniversalIntegrator.Ftp;
using SBAST.UniversalIntegrator.Helpers;
using SBAST.UniversalIntegrator.Modules;
using SBAST.UniversalIntegrator.Services;
using SBAST.UniversalIntegrator.Zip;
using NLog;
using Quartz;
using Quartz.Impl;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.ServiceProcess;
using SBAST.UniversalIntegrator.Services.Converters;
using SBAST.UniversalIntegrator.Models;
using System.IO;
using System.Threading;



namespace SBAST.UniversalIntegrator
{
    /// <summary>
    /// Основной класс приложения
    /// </summary>
    class ZakupkiService //: ServiceBase
    {
        static void Main()
        {
            //try
            //{

                var zakupkiService = new ZakupkiService();
                zakupkiService.Init();
                Thread.Sleep(Timeout.Infinite);


                //Console.ReadKey();
            //}
            //catch (Exception ex)
            //{
            //    Console.WriteLine(ex);
            //    // Console.ReadKey();
            //}
        }

        /// <summary>
        /// Инициализация Модулей
        /// </summary>

        public async void Init()
        {
            var configManager = new ConfigManager();
            var config = configManager.GetConfig();
            InitLogger(config);
            // Grab the Scheduler instance from the Factory
            var props = new NameValueCollection
            {
                { "quartz.serializer.type", "binary" },
                { "quartz.threadPool.threadCount", "16" }
            };
            var factory = new StdSchedulerFactory(props);
            var scheduler = await factory.GetScheduler();

            // and start it off
            await scheduler.Start();

            config.Modules.ForEach(module =>
            {
                var jobContainer = SetupContainer(module);
                IDictionary<string, object> jobParameters = new Dictionary<string, object>() { { "container", jobContainer } };
                var job = CreateModule(module)
                    .WithIdentity(module.ModuleName, module.ModuleName)
                    .UsingJobData(new JobDataMap(jobParameters))
                    .Build();

                var trigger = TriggerBuilder.Create()
                    .WithIdentity(module.ModuleName, module.ModuleName)
                    .StartNow()
                    .WithCronSchedule(module.Interval)
                    .Build();

                scheduler.ScheduleJob(job, trigger).Wait();
            });
        }


        /// <summary>
        /// Конфигурация контейнера 
        /// </summary>
        /// <param name="module">Метадата модуля</param>
        /// <returns></returns>
        private Container SetupContainer(Module module)
        {
            var container = new Container();
            var logger = LogManager.GetLogger($"{module.ModuleType}:{module.ModuleName}");
            container.RegisterInstance<ILogger>(logger);
            switch (module.ModuleType)
            {
                case ModuleTypeEnum.FtpDownloader:
                    var prms = (FtpDownloaderParameters)module.Parameters;
                    container.Register<IFtpClient, FtpClientSingleton>(Reuse.Scoped, made: Parameters.Of.Name("ftpAddress", defaultValue: prms.FtpAddress)
                        .OverrideWith(Parameters.Of.Name("ftpLogin", defaultValue: prms.FtpLogin))
                        .OverrideWith(Parameters.Of.Name("ftpPassword", defaultValue: prms.FtpPassword))
                        .OverrideWith(Parameters.Of.Name("requestDelay", defaultValue: prms.RequestDelay)));
                    container.Register<IArchiveRepository, ArchiveRepository>(Reuse.Scoped, made: Parameters.Of.Name("connectionString", defaultValue: prms.ConnectionString)
                        .OverrideWith(Parameters.Of.Name("connectionTimeout", defaultValue: prms.ConnectionTimeout)));
                    container.Register<IFilesDownloader, FilesDownloader>(Reuse.Scoped, made: Parameters.Of.Name("ftpDownloadFolder", defaultValue: prms.FtpDownloadFolder)
                        .OverrideWith(Parameters.Of.Name("ftpDownloadFolderIgnoreList", defaultValue: prms.FtpDownloadFolderIgnoreList))
                        .OverrideWith(Parameters.Of.Name("downloadInTheLastDays", defaultValue: prms.DownloadInTheLastDays))
                        .OverrideWith(Parameters.Of.Name("downloadFolder", defaultValue: prms.DownloadFolder))
                        .OverrideWith(Parameters.Of.Name("regexPatternByDate", defaultValue: prms.RegexPatternByDate))
                        .OverrideWith(Parameters.Of.Name("folderStructure", defaultValue: prms.FolderStructure)));
                    container.Register<ICheckSumService, CheckSumService>(Reuse.Singleton);
                    break;
                case ModuleTypeEnum.FtpSingleFileDownloader:
                    var singleDownloaderPrms = (FtpSingleFileDownloaderParameters)module.Parameters;
                    container.Register<IFtpClient, FtpClientSingleton>(Reuse.Scoped, made: Parameters.Of.Name("ftpAddress", defaultValue: singleDownloaderPrms.FtpAddress)
                        .OverrideWith(Parameters.Of.Name("ftpLogin", defaultValue: singleDownloaderPrms.FtpLogin))
                        .OverrideWith(Parameters.Of.Name("ftpPassword", defaultValue: singleDownloaderPrms.FtpPassword))
                        .OverrideWith(Parameters.Of.Name("requestDelay", defaultValue: singleDownloaderPrms.RequestDelay)));
                    container.Register<IArchiveRepository, ArchiveRepository>(Reuse.Scoped, made: Parameters.Of.Name("connectionString", defaultValue: singleDownloaderPrms.ConnectionString)
                        .OverrideWith(Parameters.Of.Name("connectionTimeout", defaultValue: singleDownloaderPrms.ConnectionTimeout)));
                    container.Register<IFilesDownloader, SingleFilesDownloader>(Reuse.Scoped, made: Parameters.Of.Name("downloadFolder", defaultValue: singleDownloaderPrms.DownloadFolder)
                        .OverrideWith(Parameters.Of.Name("ftpDownloadFileList", defaultValue: singleDownloaderPrms.FtpDownloadFileList)));
                    container.Register<ICheckSumService, CheckSumService>(Reuse.Singleton);
                    break;
                case ModuleTypeEnum.DBParser:
                    var dbParserPrms = (DBParserParameters)module.Parameters;
                    container.Register<IFilesRepository, FilesRepository>(Reuse.Scoped, made: Parameters.Of.Name("connectionString", defaultValue: dbParserPrms.ConnectionString)
                        .OverrideWith(Parameters.Of.Name("connectionTimeout", defaultValue: dbParserPrms.ConnectionTimeout))
                        .OverrideWith(Parameters.Of.Name("parsingProcedure", defaultValue: dbParserPrms.ParsingProcedure))
                        .OverrideWith(Parameters.Of.Name("filesGetProcedure", defaultValue: dbParserPrms.FilesGetProcedure)));
                    container.Register<IParserPlanService, ParserPlanService>(Reuse.Scoped, made: Parameters.Of.Name("entityType", defaultValue: dbParserPrms.EntityType)
                        .OverrideWith(Parameters.Of.Name("batchSize", defaultValue: dbParserPrms.BatchSize)));
                    container.Register<ISignVerificationService, SignVerificationService>(Reuse.Singleton);
                    break;
                //case ModuleTypeEnum.DBParserPlan:
                //    var dbParserPlanPrms = (DBParserPlanParameters)module.Parameters;
                //    container.Register<IFilesRepository, FilesRepository>(Reuse.Scoped, made: Parameters.Of.Name("connectionString", defaultValue: dbParserPlanPrms.ConnectionString)
                //        .OverrideWith(Parameters.Of.Name("connectionStringPlan", defaultValue: dbParserPlanPrms.ConnectionStringPlan))
                //        .OverrideWith(Parameters.Of.Name("connectionTimeout", defaultValue: dbParserPlanPrms.ConnectionTimeout))
                //        .OverrideWith(Parameters.Of.Name("parsingProcedurePlan", defaultValue: dbParserPlanPrms.ParsingProcedurePlan))
                //        .OverrideWith(Parameters.Of.Name("parsingProcedurePosition", defaultValue: dbParserPlanPrms.ParsingProcedurePosition))
                //        .OverrideWith(Parameters.Of.Name("filesGetProcedure", defaultValue: dbParserPlanPrms.FilesGetProcedure)));
                //    container.Register<IParserService, ParserService>(Reuse.Scoped, made: Parameters.Of.Name("entityType", defaultValue: dbParserPlanPrms.EntityType)
                //        .OverrideWith(Parameters.Of.Name("batchSize", defaultValue: dbParserPlanPrms.BatchSize)));
                //    container.Register<ISignVerificationService, SignVerificationService>(Reuse.Singleton);
                //    break;
                case ModuleTypeEnum.UnArchiver:
                    var unarchiverPrms = (UnArchiverParameters)module.Parameters;
                    container.Register<IArchiveRepository, ArchiveRepository>(Reuse.Scoped, made: Parameters.Of.Name("connectionString", defaultValue: unarchiverPrms.ConnectionString)
                        .OverrideWith(Parameters.Of.Name("connectionTimeout", defaultValue: unarchiverPrms.ConnectionTimeout)));
                    container.Register<IFilesRepository, FilesRepository>(Reuse.Scoped, made: Parameters.Of.Name("connectionString", defaultValue: unarchiverPrms.ConnectionString)
                        .OverrideWith(Parameters.Of.Name("connectionTimeout", defaultValue: unarchiverPrms.ConnectionTimeout))
                        .OverrideWith(Parameters.Of.Name("parsingProcedure", defaultValue: ""))
                        .OverrideWith(Parameters.Of.Name("filesGetProcedure", defaultValue: "")));
                    container.Register<IZipService, ZipService>(Reuse.Scoped, made: Parameters.Of.Name("downloadFolder", defaultValue: unarchiverPrms.FolderFrom)
                        .OverrideWith(Parameters.Of.Name("removeNamespaces", defaultValue: unarchiverPrms.RemoveNamespaces))
                        .OverrideWith(Parameters.Of.Name("entityType", defaultValue: unarchiverPrms.EntityType))
                        .OverrideWith(Parameters.Of.Name("maxDegreeOfParallelism", defaultValue: unarchiverPrms.MaxDegreeOfParallelism))
                        .OverrideWith(Parameters.Of.Name("folderStructure", defaultValue: unarchiverPrms.FolderStructure))
                        .OverrideWith(Parameters.Of.Name("fileFormat", defaultValue: unarchiverPrms.ConverterParameters.FileFormat))
                        .OverrideWith(Parameters.Of.Name("entityTypeFromFilename", defaultValue: unarchiverPrms.EntityTypeFromFilename))
                        .OverrideWith(Parameters.Of.Type<IFileConverterService>(serviceKey: unarchiverPrms.ConverterParameters.FileFormat, defaultValue: null, ifUnresolved: IfUnresolved.ReturnDefaultIfNotRegistered)));
                    container.Register<ICheckSumService, CheckSumService>(Reuse.Singleton);

                    // Конвертеры файлов резолвятся на основании формата файла из параметров
                    container.Register<IFileConverterService, CsvToXmlConverter>(Reuse.Scoped, serviceKey: FileFormatEnum.Csv,
                        made: Parameters.Of.Name("parameters", defaultValue: unarchiverPrms.ConverterParameters as CsvToXmlParameters));

                    break;
                case ModuleTypeEnum.TestModule:
                    var tmParams = (TestModuleParameters)module.Parameters;
                    break;
            }
            return container;
        }
        /// <summary>
        /// Создание инстанса модуля
        /// </summary>
        /// <param name="module">Метадата модуля</param>
        /// <returns></returns>
        private static JobBuilder CreateModule(Module module)
        {
            switch (module.ModuleType)
            {
                case ModuleTypeEnum.FtpDownloader:
                    return JobBuilder.Create<FtpDownloaderModule>();
                case ModuleTypeEnum.DBParser:
                    return JobBuilder.Create<DBParserModule>();
                case ModuleTypeEnum.UnArchiver:
                    return JobBuilder.Create<UnArchiverModule>();
                case ModuleTypeEnum.TestModule:
                    return JobBuilder.Create<TestModule>();
                case ModuleTypeEnum.FtpSingleFileDownloader:
                    return JobBuilder.Create<FtpSingleFileDownloader>();
                //case ModuleTypeEnum.DBParserPlan:
                //    return JobBuilder.Create<DBParserPlanModule>();
            }
            return null;
        }
        /// <summary>
        /// Инициализация логера
        /// </summary>
        /// <param name="config">Конфиг для настройки</param>
        /// <returns></returns>
        private Logger InitLogger(Config config)
        {
            string configPath = Directory.GetCurrentDirectory();//Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            configPath = Path.Combine(configPath, "nlog.config");
            LogManager.Configuration = new NLog.Config.XmlLoggingConfiguration(configPath);

            //NLog.LogManager.LoadConfiguration("UniversalIntegrator.exe.config");
            return LogManager.GetCurrentClassLogger();
        }

    }
}
