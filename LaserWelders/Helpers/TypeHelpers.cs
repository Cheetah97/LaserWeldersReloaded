using System;
using System.Collections.Generic;
using System.Linq;

namespace EemRdx.LaserWelders.Helpers
{
    public static class TypeHelpers
    {
        /// <summary>
        /// Checks if the given object is of given type.
        /// </summary>
        public static bool IsOfType<T>(this object Object, out T Casted) where T : class
        {
            Casted = Object as T;
            return Casted != null;
        }

        public static bool IsOfType<T>(this object Object) where T : class
        {
            return Object is T;
        }

        public static void AddOrUpdate<TKey, TValue>(this IDictionary<TKey, TValue> Dict, TKey Key, TValue Value)
        {
            if (Dict.ContainsKey(Key)) Dict[Key] = Value;
            else Dict.Add(Key, Value);
        }

        public static void RemoveAll<TKey, TValue>(this Dictionary<TKey, TValue> Dict, IEnumerable<TKey> RemoveKeys)
        {
            if (RemoveKeys == null || RemoveKeys.Count() == 0) return;
            foreach (TKey Key in RemoveKeys)
                Dict.Remove(Key);
        }

        public static bool Any<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> Filter, out TSource First)
        {
            First = source.FirstOrDefault(Filter);
            return !First.Equals(default(TSource));
        }

        public static HashSet<T> ToHashSet<T>(this ICollection<T> Enum)
        {
            var Hashset = new HashSet<T>(Enum);
            if (Hashset.Count > 0 && Enum.Count > 0) return Hashset;
            foreach (var Item in Enum)
                Hashset.Add(Item);
            return Hashset;
        }

        public static HashSet<T> ToHashSet<T>(this IEnumerable<T> Enum)
        {
            var Hashset = new HashSet<T>(Enum);
            if (Hashset.Count > 0 && Enum.Count() > 0) return Hashset;
            foreach (var Item in Enum)
                Hashset.Add(Item);
            return Hashset;
        }

        /// <summary>
        /// Sorts out an enumerable into lists of different types using a single loop.
        /// <para />
        /// This method is suited for 2 types.
        /// </summary>
        public static void SortByType<TI, TO1, TO2>(this IEnumerable<TI> Collection, ICollection<TO1> Type1, ICollection<TO2> Type2) where TI : class where TO1 : class, TI where TO2 : class, TI
        {
            foreach (TI Item in Collection)
            {
                TO1 Type1Item = Item as TO1;
                TO2 Type2Item = Item as TO2;
                if (Type1Item != null) Type1.Add(Type1Item);
                if (Type2Item != null) Type2.Add(Type2Item);
            }
        }

        /// <summary>
        /// Sorts out an enumerable into lists of different types using a single loop.
        /// <para />
        /// This method is suited for 3 types.
        /// </summary>
        public static void SortByType<TI, TO1, TO2, TO3>(this IEnumerable<TI> Collection, ICollection<TO1> Type1, ICollection<TO2> Type2, ICollection<TO3> Type3) where TI : class where TO1 : class, TI where TO2 : class, TI where TO3 : class, TI
        {
            foreach (TI Item in Collection)
            {
                TO1 Type1Item = Item as TO1;
                TO2 Type2Item = Item as TO2;
                TO3 Type3Item = Item as TO3;
                if (Type1Item != null) Type1.Add(Type1Item);
                if (Type2Item != null) Type2.Add(Type2Item);
                if (Type3Item != null) Type3.Add(Type3Item);
            }
        }

        /// <summary>
        /// Sorts out an enumerable into lists of different types using a single loop.
        /// <para />
        /// This method is suited for 4 types.
        /// </summary>
        public static void SortByType<TI, TO1, TO2, TO3, TO4>(this IEnumerable<TI> Collection, ICollection<TO1> Type1, ICollection<TO2> Type2, ICollection<TO3> Type3, ICollection<TO4> Type4) where TI : class where TO1 : class, TI where TO2 : class, TI where TO3 : class, TI where TO4 : class, TI
        {
            foreach (TI Item in Collection)
            {
                TO1 Type1Item = Item as TO1;
                TO2 Type2Item = Item as TO2;
                TO3 Type3Item = Item as TO3;
                TO4 Type4Item = Item as TO4;
                if (Type1Item != null) Type1.Add(Type1Item);
                if (Type2Item != null) Type2.Add(Type2Item);
                if (Type3Item != null) Type3.Add(Type3Item);
                if (Type4Item != null) Type4.Add(Type4Item);
            }
        }

        public static Object GetData<Key, Object>(this Dictionary<Key, Object> Dict, Key Tag)
        {
            return Dict[Tag];
        }

        /// <summary>
        /// Takes an enumerable and returns a dictionary.
        /// Key is the object, and value is the number of occurrences.
        /// </summary>
        public static Dictionary<T, int> CollapseDuplicates<T>(this IEnumerable<T> Enum)
        {
            Dictionary<T, int> DupeList = new Dictionary<T, int>(Enum.Count());
            if (Enum.Count() == 0) return DupeList;
            if (Enum.Count() == 1)
            {
                DupeList.Add(Enum.First(), 1);
                return DupeList;
            }
            foreach (T Item in Enum)
            {
                if (!DupeList.ContainsKey(Item)) DupeList.Add(Item, 1);
                else DupeList[Item] += 1;
            }
            return DupeList;
        }

        public static IList<T> Except<T>(this IList<T> Enum, T Exclude)
        {
            Enum.Remove(Exclude);
            return Enum;
        }

        public static List<T> Except<T>(this List<T> Enum, T Exclude)
        {
            Enum.Remove(Exclude);
            return Enum;
        }

        public static HashSet<T> Except<T>(this HashSet<T> Enum, T Exclude)
        {
            Enum.Remove(Exclude);
            return Enum;
        }
    }

}
