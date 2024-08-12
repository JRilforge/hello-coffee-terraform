using System;
using Constructs;
using HashiCorp.Cdktf;
using MyTerraformStack;

namespace MyCompany.MyApp
{
    class Program
    {
        public static void Main(string[] args)
        {
            App app = new App();
            new MainStack(app, "hello-coffee-terraform");
            app.Synth();
            Console.WriteLine("App synth complete");
        }
    }
}