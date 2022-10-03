using Markdig.Helpers;
using Newtonsoft.Json.Linq;
using Outrage.TokenParser;
using Outrage.TokenParser.Matchers;
using Outrage.TokenParser.Tokens;
using Outrage.Verge.Parser;
using Outrage.Verge.Parser.Tokens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Outrage.Verge.Processor
{
    public class Variables
    {
        private Variables? parent = null;
        private readonly Dictionary<string, object?> values = new();

        public Variables(IDictionary<string, object?> variables)
        {
            foreach (var variable in variables)
            {
                values[variable.Key] = variable.Value;
            }
        }

        public Variables(params (string key, object? value)[] variables)
        {
            foreach (var variable in variables)
            {
                values[variable.key] = variable.value;
            }
        }

        public Variables(params KeyValuePair<string, object?>[] variables)
        {
            foreach (var variable in variables)
            {
                values[variable.Key] = variable.Value;
            }
        }

        public Variables(Variables variables, Variables? parent = null) : this(variables.values)
        {
            this.parent = parent;
        }

        public Variables Combine(Variables? variables)
        {
            var result = new Variables(this.values);
            if (variables != null)
            {
                foreach (var variable in variables.values)
                {
                    result.values[variable.Key] = variable.Value;
                }
            }

            return result;
        }

        public bool HasParent => this.parent != null;
        public Variables? Parent => this.parent;
        public IDictionary<string, object?> Locals => this.values;

        public bool HasValue(string name)
        {
            if (name.Contains('.'))
            {
                var variableChain = name.Split('.');
                var variableName = variableChain.Take(1).Single();
                return this.HasValue(variableName);
            }
            else
                return this.values.ContainsKey(name) || (parent?.HasValue(name) ?? false);
        }

        public object? GetValue(string name)
        {
            return GetValue<object?>(name) ?? parent?.GetValue(name) ?? null;
        }

        private object? TraverseModel(object? model, IEnumerable<string> additionalElements)
        {
            if (!additionalElements.Any())
                return model;

            var name = additionalElements.First();
            var type = model?.GetType();
            var property = type?.GetProperty(name);
            if (property != null)
            {
                var innerModel = property.GetValue(model);
                return TraverseModel(innerModel, additionalElements.Skip(1));
            }
            else
            {
                return null;
            }
        }

        private object? TraverseJObject(JObject model, IEnumerable<string> additionalElements)
        {
            var elementName = additionalElements.First();
            if (elementName == null)
                throw new ArgumentException("No inner object found");

            if (model.TryGetValue(elementName, out JToken? innerToken))
            {
                if (innerToken is JValue)
                {
                    var jValue = (JValue)innerToken;
                    var propertyValue = jValue.Value;
                    if (additionalElements.Count() == 1)
                    {
                        return propertyValue;
                    }
                    else
                    {
                        throw new ArgumentException($"{elementName} does not contain any inner properties.");
                    }
                }
                if (innerToken is JArray)
                {
                    var propertyValue = (JArray)innerToken;
                    if (additionalElements.Count() == 1)
                    {
                        return propertyValue.Values<dynamic>();
                    }
                    else
                    {
                        throw new ArgumentException($"{elementName} does not contain any inner properties.");
                    }
                }
                if (innerToken is JObject)
                {
                    var propertyValue = (JObject)innerToken;
                    if (additionalElements.Count() == 1)
                    {
                        return propertyValue;
                    }
                    else
                    {
                        return TraverseJObject(propertyValue, additionalElements.Skip(1));
                    }
                }
            }

            return null;
            //throw new ArgumentException($"Could not find property {elementName}.");
        }

        public T? GetValue<T>(string name)
        {
            if (name.Contains('.'))
            {
                var variableChain = name.Split('.');
                var variableName = variableChain.Take(1).Single();
                var additionalElements = variableChain.Skip(1);

                var model = this.GetValue(variableName);
                if (model is JObject)
                    return (T?)TraverseJObject((JObject)model, additionalElements);
                else
                    return (T?)TraverseModel(model, additionalElements);
            }
            else
            {
                if (this.values.TryGetValue(name, out var valueResult))
                {
                    return (T?)valueResult;
                }
                else if (this.parent != null)
                {
                    return this.parent.GetValue<T>(name);
                }
                else
                    return default(T);
            }
        }

        public T SetValue<T>(string name, T value)
        {
            this.values[name] = value;
            return value;
        }

        static IMatcher variableMatcher = Matcher.FirstOf(
            HTMLParser.Variable,
            Characters.AnyChar
        ).Many().Then(Controls.EndOfFile);


        public string ReplaceVariables(string input)
        {
            var match = TokenParser.TokenParser.Parse(input, variableMatcher);
            if (match.Success)
            {
                StringBuilder resultBuilder = new StringBuilder();
                foreach (var token in match.Tokens)
                {
                    if (token is VariableToken)
                    {
                        var variableToken = (VariableToken)token;
                        if (HasValue(variableToken.VariableName))
                        {
                            var value = GetValue(variableToken.VariableName);
                            if (value is string && value != null)
                            {
                                value = this.ReplaceVariables((string)value);
                            }
                            resultBuilder.Append(value);
                        }
                    }
                    if (token is StringValueToken)
                    {
                        var stringValueToken = (StringValueToken)token;
                        resultBuilder.Append(stringValueToken.Value);
                    }
                }

                return resultBuilder.ToString();
            }
            else
            {
                throw new ArgumentException(match.Error);
            }
        }

        public static Variables Empty
        {
            get
            {
                {
                    return new Variables(new Dictionary<string, object?>());
                }
            }
        }
    }
}