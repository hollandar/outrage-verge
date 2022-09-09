using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Outrage.Verge.Processor
{
    public class SpecialEnumerable<TType> : IEnumerable<TType>
    {
        private readonly IEnumerable<TType> enumerable;

        public SpecialEnumerable(IEnumerable<TType> enumerable)
        {
            this.enumerable = enumerable;
        }

        public IEnumerator<TType> GetEnumerator()
        {
            var enumerator = new SpecialEnumerator<TType>(this.enumerable);
            return enumerator;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }
    public class SpecialEnumerator<TType> : IEnumerator<TType>
    {
        private readonly IEnumerator<TType> enumerator;

        public SpecialEnumerator(IEnumerable<TType> enumerable)
        {
            this.enumerator = enumerable.GetEnumerator();
        }

        public TType Current => enumerator.Current;

        object IEnumerator.Current => this.Current!;

        public void Dispose()
        {
            this.enumerator.Dispose();
        }

        public bool MoveNext()
        {
            return enumerator.MoveNext();
        }

        public void Reset()
        {
            this.enumerator.Reset();
        }

        public IEnumerable<TType> TakeUntil<TUntilType>(Func<TUntilType?, bool> untilFunc) where TUntilType : class
        {
            while (this.enumerator.MoveNext())
            {
                if (this.enumerator.Current is TUntilType && untilFunc(this.enumerator.Current as TUntilType))
                { break; }

                yield return this.enumerator.Current;
            }
        }
    }
}
