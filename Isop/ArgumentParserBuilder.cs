﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace Isop
{
    public class ArgumentParserBuilder
    {
        private readonly IList<ArgumentWithOptions> _argumentRecognizers;
        private readonly IList<ControllerRecognizer> _controllerRecognizers;
        private CultureInfo _cultureInfo;
        private TypeConverterFunc _typeConverter;
        private HelpForControllers _helpForControllers;
        private HelpForArgumentWithOptions _helpForArgumentWithOptions;
        private HelpController _helpController;
        private readonly TypeContainer _container=new TypeContainer();
        public ArgumentParserBuilder()
        {
            _controllerRecognizers = new List<ControllerRecognizer>();
            _argumentRecognizers = new List<ArgumentWithOptions>();
        }

        public ArgumentParserBuilder Parameter(ArgumentParameter argument, Action<string> action = null, bool required = false, string description = null)
        {
            _argumentRecognizers.Add(new ArgumentWithOptions(argument, action, required, description));
            return this;
        }
        /// <summary>
        /// Sets the cultureinfo for the following calls.
        /// </summary>
        /// <param name="cultureInfo"></param>
        /// <returns></returns>
        public ArgumentParserBuilder SetCulture(CultureInfo cultureInfo)
        {
            _cultureInfo = cultureInfo; return this;
        }
        public ArgumentParserBuilder SetTypeConverter(TypeConverterFunc typeconverter)
        {
            _typeConverter = typeconverter; return this;
        }
		
		public ArgumentParserBuilder SetFactory(Func<Type,Object> factory)
		{
		    _container.Factory=factory;
		    return this;
		}
		
        public ParsedArguments Parse(IEnumerable<string> arg)
        {
			var argumentParser = new ArgumentParser(_argumentRecognizers);

            var lexer = new ArgumentLexer(arg);
            var parsedArguments = argumentParser.Parse(lexer, arg);
            if (_controllerRecognizers.Any())
            {
                var controllerRecognizer = _controllerRecognizers.FirstOrDefault(recognizer => recognizer.Recognize(arg));
                if (null != controllerRecognizer)
                {
					var parsedMethod = controllerRecognizer.Parse(arg);
					parsedMethod.Factory = _container.CreateInstance;
                    var merged = parsedArguments.Merge( parsedMethod);
                    //TODO: This is a hack! Should have some better way of controlling this!
                    if (parsedMethod.RecognizedAction == null ||
                        !parsedMethod.RecognizedAction.ReflectedType.Equals(typeof (HelpController)))
                        FailOnUnMatched(merged);
                    return merged;
                }
            }
            FailOnUnMatched(parsedArguments);
            return parsedArguments;
        }

        private static void FailOnUnMatched(ParsedArguments parsedArguments)
        {
            var unMatchedRequiredArguments = parsedArguments.UnMatchedRequiredArguments();

            if (unMatchedRequiredArguments.Any())
            {
                throw new MissingArgumentException("Missing arguments")
                          {
                              Arguments = unMatchedRequiredArguments
                                  .Select(
                                      unmatched =>
                                      new KeyValuePair<string, string>(unmatched.Argument.ToString(), unmatched.Argument.Help()))
                                  .ToList()
                          };
            }
        }

        public ArgumentParserBuilder Recognize(Type arg, CultureInfo cultureInfo = null, TypeConverterFunc typeConverter = null)
        {
            _controllerRecognizers.Add(new ControllerRecognizer(arg, _cultureInfo ?? cultureInfo, _typeConverter ?? typeConverter));
            return this;
        }
		public ArgumentParserBuilder Recognize(Object arg, CultureInfo cultureInfo = null, TypeConverterFunc typeConverter = null)
        {
            _controllerRecognizers.Add(new ControllerRecognizer(arg.GetType(), _cultureInfo ?? cultureInfo, _typeConverter ?? typeConverter));
            _container.Instances.Add(arg.GetType(),arg);
            return this;
        }
		

        public String Help()
        {
            var cout = new StringWriter(_cultureInfo);
            Parse(new []{"Help"}).Invoke(cout);
			return cout.ToString();// return _helpController.Index();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="theCommandsAre">default: "The commands are:"</param>
        /// <param name="helpCommandForMoreInformation">default: "Se 'COMMANDNAME' help command for more information"</param>
        /// <param name="theSubCommandsFor">default: The sub commands for </param>
        /// <param name="helpSubCommandForMoreInformation">default: Se 'COMMANDNAME' help 'command' 'subcommand' for more information</param>
        /// <returns></returns>
        public ArgumentParserBuilder HelpTextCommandsAre(string theCommandsAre,
            string helpCommandForMoreInformation,
            string theSubCommandsFor,
            string helpSubCommandForMoreInformation)
        {
            RecognizeHelp();
            _helpForControllers.TheCommandsAre = theCommandsAre;
            _helpForControllers.HelpCommandForMoreInformation = helpCommandForMoreInformation;
            _helpForControllers.TheSubCommandsFor = theSubCommandsFor;
            _helpForControllers.HelpSubCommandForMoreInformation = helpSubCommandForMoreInformation;
            return this;
        }
        
        public ArgumentParserBuilder HelpTextArgumentsAre(string theArgumentsAre)
        {
            RecognizeHelp();
            _helpForArgumentWithOptions.TheArgumentsAre = theArgumentsAre;
            return this;
        }

        public string HelpFor(string command)
        {
            var cout = new StringWriter(_cultureInfo);
            Parse(new[] { "Help", command }).Invoke(cout);
            return cout.ToString();//_helpController.Index(command);
        }

        public ArgumentParserBuilder RecognizeHelp()
        {
            if (_helpController==null)
            {
                _helpForControllers = new HelpForControllers(_controllerRecognizers, _container);
                _helpForArgumentWithOptions = new HelpForArgumentWithOptions(_argumentRecognizers);
                _helpController = new HelpController(_helpForArgumentWithOptions, _helpForControllers);
                Recognize(_helpController);
            }
            return this;
        }

        public IEnumerable<ControllerRecognizer> GetControllerRecognizers()
        {
            return _controllerRecognizers;
        }

        public IEnumerable<ArgumentWithOptions> GetGlobalParameters()
        {
            return _argumentRecognizers;
        }

        public Func<Type, object> GetFactory()
        {
            return _container.CreateInstance;
        }
    }
}