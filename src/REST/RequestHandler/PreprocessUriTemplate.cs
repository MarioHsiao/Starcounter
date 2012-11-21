
using System;
using System.Collections.Generic;
using System.Text;
namespace Starcounter.Internal.Uri {

    /// <summary>
    /// Translates a verb and uri handler template into a so called prepared template.
    /// A prepared template is a single string combining the http method (verb), the uri
    /// and the parameter types.
    /// 
    /// For instance, the template GET("/products/{?}", (int prodno) => {...} ) translates
    /// into the prepared template "GET /products/@s".
    /// 
    /// The @s corresponds to the {?}. The 's' denotes type and the '@' makes it simpler
    /// for the parser generator to parse parameter positions.
    /// </summary>
    /// <remarks>
    /// The grammar for an uri template is as follows:
    /// 
    /// UriTemplate = ( urifragement [ '{' parametertemplate '}' ] *)
    /// urifragment = (urichar*)
    /// parametertemplate = ? | identifier
    /// urichar = ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789-._~:/?#[]@!$ andsign '()*+,;=
    /// identifier = firstidchar | ( restidchar *)
    /// firstidchar = ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz_
    /// restidchar = firstidchar | 0123456789_
    /// 
    /// The parametertemplate will grow more complex in the final product.
    /// </remarks>
    public static class UriTemplatePreprocessor {

        /// <summary>
        /// Performs the translation as described in the class summary.
        /// </summary>
        /// <param name="rp">The meta data object corresponding to the user handler.
        /// See also the summary in the class.</param>
        /// <returns>A single string that contains everything needed to match an
        /// incomming verb and uri in a http request. I.e. the "GET /products/@s" as
        /// described in the class summary.</returns>
        public static string PreprocessUriTemplate( RequestProcessorMetaData rp ) {
            var str = rp.UnpreparedVerbAndUri;
            var ret = new StringBuilder();
            var state = ParseState.InFragment;
            StringBuilder parameter = null;
            int parindex = -1;
            foreach (char c in str) {
                switch (state) {
                    case (ParseState.InFragment):
                        if (c == '{') {
                            parindex++;
                            state = ParseState.InParameter;
                            ret.Append('@');
                            parameter = new StringBuilder();
                        }
                        else
                            ret.Append(c);
                        break;
                    case (ParseState.InParameter):
                        if (c == '}') {
                            ret.Append(TypeToChar(str,rp.ParameterTypes,parindex));
                            state = ParseState.InFragment;
                            var p = parameter.ToString();
                            if ( p != "?")
                                throw new Exception(
                                    String.Format("Error in the URI template {0}. Parameters are denoted by {1}. I.e. the parameters must be named \"?\", not \"{2}\".",
                                    str, "{?}", p
                                    ));
                        }
                        else {
                            parameter.Append(c);
                        }
                        break;
                }
            }
            return ret.ToString() +" ";
        }

        /// <summary>
        /// In the prepared template string, each parameter type has a specific character
        /// following the @ character.
        /// </summary>
        /// <param name="templ">The original URI template of the user handler</param>
        /// <param name="types">All the .NET types in the user handler</param>
        /// <param name="parindex">Type type in the list to find the type character for</param>
        /// <returns>The type character. For instance, the input type String will return 's'.</returns>
        private static char TypeToChar(string templ, List<Type> types, int parindex ) {
            Type type = types[parindex];
            if (type == typeof(string))
                return 's';
            if (type == typeof(int))
                return 'i';

            string pars = null;
            int i = 0;
            foreach (Type t in types) {                
                if (pars == null)
                    pars = "(";
                else
                    pars += ",";
                pars += t.Name + " p" + (++i);
            }
            pars += ")";
            throw new Exception(
                String.Format("Error in the URI template {0} {1}. The type {2} is not supported.",
                templ, pars, type.Name
                ));
            throw new NotImplementedException("Add more URI parameter types here");
        }

        /// <summary>
        /// The simple grammar in the current version is simply handled by a simple finite state machine.
        /// </summary>
        private enum ParseState {
            InFragment,
            InParameter
        }
    }
}
