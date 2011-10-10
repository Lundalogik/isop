﻿using System;
using System.Linq;
using Isop;

namespace Isop.Example
{
    /// <summary>
    /// This is a sample usage of console helpers:
    /// </summary>
    class Program
    {
        static void Main(string[] args)
        {
            //TODO: This should be shorter!
            var parserBuilder = ArgumentParser.Build()
                       .RecognizeHelp()
                       .Recognize(typeof(MyController))
                       .Recognize(typeof(CustomerController));
            try
            {
                var parsedMethod = parserBuilder.Parse(args);
                if (parsedMethod.UnRecognizedArguments.Any())//Warning:
                {
                    var unRecognizedArgumentsMessage = string.Format(
@"Unrecognized arguments: 
{0}
Did you mean any of these arguments?
{1}", String.Join(",", parsedMethod.UnRecognizedArguments.Select(unrec => unrec.Value).ToArray()),
      String.Join(",", parsedMethod.ArgumentWithOptions.Select(rec => rec.Argument.ToString()).ToArray()));
                    Console.WriteLine(unRecognizedArgumentsMessage);
                }
                Console.WriteLine(parsedMethod.Invoke());
            }
            catch (MissingArgumentException)
            {
                Console.WriteLine("Missing argument(s)");

                Console.WriteLine(parserBuilder.Help());
            }
            catch (NoClassOrMethodFoundException)
            {
                Console.WriteLine("Missing argument(s) or wrong argument(s)");

                Console.WriteLine(parserBuilder.Help());
            }
        }
    }

    public class MyController
    {
        public string Action(string value)
        {
            return "invoking action on mycontroller with value : " + value;
        }
    }
    public class CustomerController
    {
        public string Add(string name)
        {
            return "invoking action Add on customercontroller with name : " + name;
        }
    }
}
