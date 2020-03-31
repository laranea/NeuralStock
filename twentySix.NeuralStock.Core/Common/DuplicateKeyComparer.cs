namespace twentySix.NeuralStock.Core.Common
{
    using System;
    using System.Collections.Generic;

    public class DuplicateKeyComparer<TKey> : IComparer<TKey> where TKey : IComparable
    {
        public int Compare(TKey x, TKey y)
        {
            if (x != null)
            {
                int result = x.CompareTo(y);

                return result == 0 ? 1 : result;
            }

            return 0;
        }
    }
}