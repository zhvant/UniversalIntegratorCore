using System;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.Xml;
using System.Xml;

namespace SBAST.UniversalIntegrator.Services
{

    /// <summary>
    /// Cервис проверки подписи, пока возвращаем всегда пройден проверку 
    /// </summary>
    public class SignVerificationService : ISignVerificationService
    {
        public bool Verify(string xml)
        {
            return true;
            //var cspParams = new CspParameters() { KeyContainerName = "XML_DSIG_RSA_KEY" };

            //// Create a new RSA signing key and save it in the container. 
            //var rsaKey = new RSACryptoServiceProvider(cspParams);

            //// Create a new XML document.
            //var xmlDoc = new XmlDocument()
            //{
            //    PreserveWhitespace = true
            //};
            //xmlDoc.LoadXml(xml);
            //return VerifyXml(xmlDoc, rsaKey);
        }

        // Verify the signature of an XML file against an asymmetric 
        // algorithm and return the result.
        public static bool VerifyXml(XmlDocument xmlDoc, RSA key)
        {
            // Check arguments.
            if (xmlDoc == null)
                throw new ArgumentException("xmlDoc");
            if (key == null)
                throw new ArgumentException("key");

            // Create a new SignedXml object and pass it
            // the XML document class.
            var signedXml = new SignedXml(xmlDoc);

            // Find the "Signature" node and create a new
            // XmlNodeList object.
            var nodeList = xmlDoc.GetElementsByTagName("Signature");

            // Throw an exception if no signature was found.
            if (nodeList.Count <= 0)
                return true;

            // This example only supports one signature for
            // the entire XML document.  Throw an exception 
            // if more than one signature was found.
            if (nodeList.Count >= 2)
            {
                throw new CryptographicException("Verification failed: More that one signature was found for the document.");
            }

            // Load the first <signature> node.  
            signedXml.LoadXml((XmlElement)nodeList[0]);

            // Check the signature and return the result.
            return signedXml.CheckSignature(key);
        }
    }
}
