using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WeTongji.Extensions.NokiaMapsSDK
{
    /// <summary>
    /// Paginateble collections are represented as individual pages (a slice of all elements of the collection) 
    /// that contain mechanisms to access the next page. 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class PaginateableCollection<T>
    {
        public IEnumerable<T> items { get; set; }

        /// <summary>
        /// The Uri to the subresource holding the next page of the collection. 
        /// If the current page is the last page of the collection, the attribute is not present. 
        /// </summary>
        public String next { get; set; }

        /// <summary>
        /// The total number of items available for a place ie. if a place has 50 images, this would be 50. 
        /// </summary>
        /// <remarks>
        /// It is not possible to always calculate the total number of elements in a collection. 
        /// So the attribute might be missing, or just contain an estimate. 
        /// Clients MUST not rely on the presence of the attribute but instead rely only on 
        /// the existence of the next attribute to find out if additional items are available.
        /// </remarks>
        public UInt32 available { get; set; }

        /// <summary>
        /// Pages other than the first page of a collection may include this attribute. 
        /// If present, it contains the index of the first item in the current page. 
        /// </summary>
        public UInt32 offset { get; set; }
    }
}
