using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Outrage.Verge.Processor
{
    public class Variables
    {
        private readonly Dictionary<string, string> values = new();

        public Variables(IDictionary<string, string> variables)
        {
            foreach (var variable in variables)
            {
                values[variable.Key] = variable.Value;
            }
        }

        public Variables(params (string key, string value)[] variables)
        {
            foreach (var variable in variables)
            {
                values[variable.key] = variable.value;
            }
        }

        public Variables(params KeyValuePair<string, string>[] variables)
        {
            foreach (var variable in variables)
            {
                values[variable.Key] = variable.Value;
            }
        }

        public string ReplaceVariables(string input, string leadin = "$(", string trailin = ")")
        {
            foreach (var variable in this.values)
            {
                var variableName = $"{leadin}{variable.Key}{trailin}";
                if (input.Contains(variableName))
                    input = input.Replace(variableName, variable.Value);
            }

            return input;
        }
    }
}
