using Newtonsoft.Json;
using System.Text;

namespace EntityMatching.Core.Utilities
{
    /// <summary>
    /// Utility for estimating the size of Cosmos DB documents in bytes.
    /// Used to determine when to split conversation documents to avoid 2MB limit.
    /// </summary>
    public static class DocumentSizeEstimator
    {
        /// <summary>
        /// Estimate the size of a document by serializing to JSON and measuring bytes
        /// </summary>
        /// <typeparam name="T">Type of document to estimate</typeparam>
        /// <param name="obj">Document object to measure</param>
        /// <returns>Estimated size in bytes</returns>
        public static long EstimateSize<T>(T obj)
        {
            var json = JsonConvert.SerializeObject(obj);
            return Encoding.UTF8.GetByteCount(json);
        }
    }
}
