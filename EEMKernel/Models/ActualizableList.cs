using System;
using System.Collections;
using System.Collections.Generic;

namespace EemRdx.Models
{
    /// <summary>
    /// Represents a self-updating collection. Use Populator to acquire data, and Validator to determine whether data should be listed.
    /// </summary>
    public class ActualizableList<T> : IReadOnlyList<T>
    {
        #region List implementation
        public T this[int index]
        {
            get { return InternalList[index]; }
        }

        public int Count => InternalList.Count;

        public IEnumerator<T> GetEnumerator()
        {
            return InternalList.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return InternalList.GetEnumerator();
        }
        #endregion
        protected readonly Func<IEnumerable<T>> Populator;
        protected readonly Func<T, bool> Validator;
        protected List<T> InternalList = new List<T>();

        /// <summary>
        /// Creates a new instance of ActualizableList. This does NOT populate the list, use Update().
        /// </summary>
        /// <param name="Populator">Acquires input data.</param>
        /// <param name="Validator">Determines which items from Populator output should be added to the list, and which no longer valid items should be removed from the list.</param>
        public ActualizableList(Func<IEnumerable<T>> Populator, Func<T, bool> Validator)
        {
            if (Populator == null) throw new Exception($"ActualizableList.constructor(): Populator function is null");
            this.Populator = Populator;
            this.Validator = Validator;
        }

        /// <summary>
        /// Updates the list, removing items which are no longer valid and adding items from input data which are valid for this instance.
        /// </summary>
        public void Update()
        {
            List<T> ForRemoval = new List<T>();
            foreach (T item in InternalList)
                if (!Validator(item)) ForRemoval.Add(item);

            InternalList.RemoveAll(ForRemoval.Contains);

            foreach (T item in Populator())
            {
                if (InternalList.Contains(item) || ForRemoval.Contains(item)) continue;
                if (Validator(item)) InternalList.Add(item);
            }
            ForRemoval.Clear();
        }
    }
}
