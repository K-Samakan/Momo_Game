using System.Collections.Generic;
using UnityEngine;

namespace LocalizationPackageExtensionsEditor
{
    internal struct Mask
    {
        public int value { get; set; }
        public static Mask all => new Mask(~0);
        public static Mask none => new Mask(0);
        public Mask(int value)
        {
            this.value = value;
        }

        public bool Has(int index)
        {
            return (value & (1 << index)) != 0;
        }

        public void Select(int index)
        {
            value = 1 << index;
        }

        public void Add(int index)
        {
            value |= 1 << index;
        }

        public void Remove(int index)
        {
            value &= ~(1 << index); 
        }

        public void Toggle(int index)
        {
            value ^= 1 << index;
        }

        public void SelectAll()
        {
            value = ~0;
        }

        public void SelectNone()
        {
            value = 0;
        }

        public List<int> GetSelectedIndices(int length)
        {
            var list = new List<int>();
            GetSelectedIndices(length, list);
            return list;
        }

        public void GetSelectedIndices(int length, List<int> indices)
        {
            indices.Clear();
            for (int i = 0; i < length; i++)
            {
                if(Has(i)) indices.Add(i);
            }
        }

        public List<T> GetSelectedItems<T>(IList<T> source)
        {
            var list = new List<T>();
            GetSelectedItems(source, list);
            return list;
        }

        public void GetSelectedItems<T>(IList<T> source, IList<T> result)
        {
            result.Clear();
            for (int i = 0; i < source.Count; i++)
            {
                if(Has(i)) result.Add(source[i]);
            }
        }

        public static int GetMaskValue(IList<int> selectedIndices)
        {
            var mask = 0;
            for (int i = 0; i < selectedIndices.Count; i++)
            {
                mask |= 1 << i;
            }

            return mask;
        }

        public static int GetMaskValue<T>(IList<T> source, IList<T> selected)
        {
            var mask = 0;
            for (int i = 0; i < source.Count; i++)
            {
                if (selected.Contains(source[i])) mask |= (1 << i);
            }

            return mask;
        }

        public static List<T> GetSelectedElements<T>(IList<T> source, int maskValue)
        {
            var selected = new List<T>();
            for (int i = 0; i < source.Count; i++)
            {
                if((maskValue & (1 << i)) != 0)
                {
                    selected.Add(source[i]);
                }
            }

            return selected;
        }
        public static void GetSelectedElements<T>(IList<T> source, IList<T> selected, int maskValue)
        {
            selected.Clear();
            for (int i = 0; i < source.Count; i++)
            {
                if((maskValue & (1 << i)) != 0)
                {
                    selected.Add(source[i]);
                }
            }
        }
        
    }

}