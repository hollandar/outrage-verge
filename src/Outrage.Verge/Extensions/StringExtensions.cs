using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;

namespace Outrage.Verge.Extensions
{
    public static class StringExtensions
    {
        public static IEnumerable<T> FromSeparatedValues<T>(this string i, string separator = ",", bool throwCastExceptions = true)
        {
            var itemList = i.Split(separator);
            foreach (var item in itemList)
            {
                T? value = default(T);
                bool cast = false;
                try
                {
                    value = (T)Convert.ChangeType(item, typeof(T));
                    cast = true;
                }
                catch (InvalidCastException e)
                {
                    if (throwCastExceptions) throw e;
                }

                if (cast)
                    yield return value!;

            }
        }
    }
}
