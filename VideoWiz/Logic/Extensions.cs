using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;
using Windows.UI.Xaml.Media.Imaging;

namespace VideoWiz.Logic
{
    public static class Extensions
    {
        public static T Next<T>(this IEnumerable<T> source, Func<T, bool> predicate)
        {
            bool flag = false;

            using (var enumerator = source.GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    if (flag) return enumerator.Current;

                    if (predicate(enumerator.Current))
                    {
                        flag = true;
                    }
                }
            }
            return default(T);
        }

        public static T Previous<T>(this IEnumerable<T> list, T current)
        {
            try
            {
                return list.TakeWhile(x => !x.Equals(current)).LastOrDefault();
            }
            catch
            {
                return default(T);
            }
        }
    }
}
