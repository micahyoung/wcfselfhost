using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel;
using System.ServiceModel.Description;
using Steeltoe.Extensions.Configuration.CloudFoundry;
using Microsoft.Extensions.Configuration;

namespace SelfHost
{
    [ServiceContract]
    public interface IHelloWorld
    {
        [OperationContract]
        void HelloWorld();

    }

    [ServiceContract]
    public interface IWriteMe
    {
        [OperationContract]
        void WriteMe(string text);
    }

    public partial class WcfEntryPoint : IHelloWorld
    {
        public void HelloWorld()
        {
            Console.WriteLine("Hello World!");
        }

    }

    public partial class WcfEntryPoint : IWriteMe
    {

        public void WriteMe(string text)
        {
            Console.WriteLine($"WriteMe: {text}");
        }
    }

    class Program
    {
        static void Main()
        {

            var builder = new ConfigurationBuilder()
                .AddCloudFoundry();

            var config = builder.Build();
            var options = new CloudFoundryApplicationOptions();
            var appSection = config.GetSection(CloudFoundryApplicationOptions.CONFIGURATION_PREFIX);
            appSection.Bind(options);


            var internalHost = "localhost";
            var internalPort = "8080";
            var publicHost = "localhost";
            var publicPort = "8080";

            if (options.ApplicationUris != null && options.ApplicationUris.Length == 1)
            {
                internalHost = options.InternalIP;
                internalPort = options.Port.ToString();

                var publicHostAndPublicPort = options.ApplicationUris.ElementAtOrDefault(0).Split(':');

                publicHost = publicHostAndPublicPort.First();
                publicPort = publicHostAndPublicPort.ElementAtOrDefault(1);
            }

            var baseAddress = new Uri($"net.tcp://{publicHost}:{publicPort}");
            var helloUri = new Uri($"net.tcp://{internalHost}:{internalPort}/IHelloWorld");
            var mexUri = new Uri($"net.tcp://{internalHost}:{internalPort}");

            Console.WriteLine($"baseAddress: {baseAddress.ToString()}");
            Console.WriteLine($"helloUri: {helloUri.ToString()}");
            Console.WriteLine($"mexUri: {mexUri.ToString()}");

            var svcHost = new ServiceHost(typeof(WcfEntryPoint), baseAddress);

            var netTcpBinding = new NetTcpBinding(SecurityMode.None);

            ServiceMetadataBehavior smb = new ServiceMetadataBehavior();
            svcHost.Description.Behaviors.Add(smb);
            svcHost.AddServiceEndpoint(
                ServiceMetadataBehavior.MexContractName,
                netTcpBinding,
                "mex",
                mexUri
            );

            svcHost.AddServiceEndpoint(
                typeof(IHelloWorld),
                netTcpBinding,
                "IHelloWorld"
            );


            svcHost.Open();
            Console.WriteLine($"svcHost is {svcHost.State}.  Press enter to close.");

            Console.ReadLine();
            svcHost.Close();
        }
    }
}

