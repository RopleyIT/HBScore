using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace DisplayScore
{
    public class BoundedList<T>
    {
        public IList<T> Items { get; set; } = new List<T>();

        public int SelectedIndex { get; private set; } = 0;

        public T Next()
        {
            if (Items == null || Items.Count <= 0)
                return default(T);
            if (SelectedIndex == Items.Count - 1)
                return Items[SelectedIndex];
            else
                return Items[++SelectedIndex];
        }

        public T Prev()
        {
            if (Items == null || Items.Count <= 0)
                return default(T);
            if (SelectedIndex == 0)
                return Items[SelectedIndex];
            else
                return Items[--SelectedIndex];
        }

        public T FirstAfterTitle()
        {
            if (Items == null || Items.Count <= 0)
                return default(T);
            if (Items.Count > 1)
                SelectedIndex = 1;
            else SelectedIndex = 0;
            return Items[SelectedIndex];
        }
    }
}
