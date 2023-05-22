The UniversalIntegrator service is a complex fault-tolerant customizable ETL solution that allows to download serialized archived XML objects from external sources, unarchive them, parse, transform, and load them into a data warehouse. 
 
This service is designed to streamline the process with guaranteed file delivery of integrating data from external sources into database systems. It can be run as console application, multithreading background service or multiple containers in Kubernetes Cluster
   
The application consists of the following configurable modules:
- The FilesDownloader module is responsible for connecting to the external data sources such as FTP server using SFTP protocol and downloading of archives with serialized XML objects.
- The UnArchiver module is responsible for unzipping packages into service database tables, collecting metadata and computing MD5 checksum.
- The Parser module runs suitable parsing functions based on an external microservices or database stored procedures.
 
All modules have error handling, data validation and deduplication algorithms ensuring that data is complete and accurate before it is integrated into data warehouse. Every module can work and be configured independently of each other.
 
Main Technologies and dependencies of the project:
.Net 6
DryIoc
Quartz.net
NLog
 
Basic Class Diagram of the project:
![image2019-6-5_15-1-5](https://github.com/zhvant/UniversalIntegratorCore/assets/53262841/cb79a2ea-3dd6-469e-9ff9-637ec124de00)

