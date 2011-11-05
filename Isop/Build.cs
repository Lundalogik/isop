﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Isop
{
    public class Build
    {
        private readonly IList<ArgumentWithOptions> _argumentRecognizers;
        private readonly IList<ControllerRecognizer> _controllerRecognizers;
        private CultureInfo _cultureInfo;
        private TypeConverterFunc _typeConverter;
        private HelpForControllers _helpForControllers;
        private HelpForArgumentWithOptions _helpForArgumentWithOptions;
        private HelpController _helpController;
        private readonly TypeContainer _container=new TypeContainer();
        public Build()
        {
            _controllerRecognizers = new List<ControllerRecognizer>();
            _argumentRecognizers = new List<ArgumentWithOptions>();
        }

        public Build Parameter(ArgumentParameter argument, Action<string> action = null, bool required = false, string description = null)
        {
            _argumentRecognizers.Add(new ArgumentWithOptions(argument, action, required, description));
            return this;
        }
        /// <summary>
        /// Sets the cultureinfo for the following calls.
        /// </summary>
        /// <param name="cultureInfo"></param>
        /// <returns></returns>
        public Build SetCulture(CultureInfo cultureInfo)
        {
            _cultureInfo = cultureInfo; return this;
        }
        public Build SetTypeConverter(TypeConverterFunc typeconverter)
        {
            _typeConverter = typeconverter; return this;
        }
		
		public Build SetFactory(Func<Type,Object> factory)
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

        public Build Recognize(Type arg, CultureInfo cultureInfo = null, TypeConverterFunc typeConverter = null)
        {
            _controllerRecognizers.Add(new ControllerRecognizer(arg, _cultureInfo ?? cultureInfo, _typeConverter ?? typeConverter));
            return this;
        }
		public Build Recognize(Object arg, CultureInfo cultureInfo = null, TypeConverterFunc typeConverter = null)
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
        public Build HelpTextCommandsAre(string theCommandsAre,
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
        
        public Build HelpTextArgumentsAre(string theArgumentsAre)
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

        public Build RecognizeHelp()
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

        public Build Configuration(Type type)
        {
            var instance = Activator.CreateInstance(type);
            var methods= type.GetMethods(BindingFlags.Instance | BindingFlags.Public);
            var recognizer = new MethodInfoFinder();
            var recognizesMethod = recognizer.Match(methods, 
                name:"Recognizes",
                returnType: typeof(IEnumerable<Type>),
                parameters: new Type[0]);
            var recognizes=(IEnumerable<Type>)recognizesMethod.Invoke(instance, new object[0]);
            foreach (var recognized in recognizes)
            {
                CultureInfo cultureInfo=null;
                TypeConverterFunc typeconverter=null;
                Recognize(recognized,cultureInfo,typeconverter);
            }
            var objectFactory = recognizer.Match(methods,
                name: "ObjectFactory",
                returnType: typeof(object),
                parameters: new[] { typeof(Type) });
            SetFactory((Func<Type, object>)Delegate.CreateDelegate(typeof(Func<Type, object>), instance, objectFactory));

            var configure = recognizer.Match(methods,
                name: "Configure",
                returnType: typeof(void));
            foreach (var parameterInfo in configure.GetParameters())
            {
                this.Parameter(parameterInfo.Name, required: true);//humz?
            }

            return this;
        }

        private class MethodInfoFinder
        {
            public MethodInfo Match(IEnumerable<MethodInfo> methods, Type returnType=null, string name=null, IEnumerable<Type> parameters=null)
            {
                IEnumerable<MethodInfo> retv=methods;
                if (null!=returnType)
                    retv = retv.Where(m => m.ReturnType == returnType);
                if (null!=parameters)
                    retv = retv.Where(m => m.GetParameters().Select(p => p.ParameterType).SequenceEqual(parameters));
                if (null!=name)
                    retv = retv.Where(m => m.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
                return retv.First();
            }
        }
    }
}