/* Author:      Chris Muench, C-Labs
 * Last Update: 4/8/2022
 * License:     MIT
 * 
 * Some contributions thanks to CESMII – the Smart Manufacturing Institute, 2021
 */

using System;

namespace CESMII.OpcUa.NodeSetImporter
{
    /// <summary>
    /// Simplified class containing all important information of a NodeSet
    /// </summary>
    public class ModelNameAndVersion
    {
        /// <summary>
        /// The main Model URI (Namespace) 
        /// </summary>
        public string ModelUri { get; set; }
        /// <summary>
        /// Version of the NodeSet
        /// </summary>
        public string ModelVersion { get; set; }
        /// <summary>
        /// Publication date of the NodeSet
        /// </summary>
        public DateTime PublicationDate { get; set; }
        /// <summary>
        /// This is not a valid OPC UA Field and might be hidden inside the "Extensions" node - not sure if its the best way to add this here
        /// </summary>
        public string Author { get; set; }
        /// <summary>
        /// Set to !=0 if this Model is an official OPC Foundation Model and points to an index in a lookup table or cloudlib id
        /// This requires a call to the CloudLib or another Model validation table listing all officially released UA Models
        /// </summary>
        public int? UAStandardModelID { get; set; }
        /// <summary>
        /// Key into the Cache Table
        /// </summary>
        public object CCacheId { get; set; }

        /// <summary>
        /// Compares two NodeSetNameAndVersion using ModelUri and Version. 
        /// </summary>
        /// <param name="thanThis">Compares this to ThanThis</param>
        /// <returns></returns>
        public bool IsNewerOrSame(ModelNameAndVersion thanThis)
        {
            if (thanThis == null)
                return false;
            return ModelUri == thanThis.ModelUri && PublicationDate >= thanThis.PublicationDate;
        }

        /// <summary>
        /// Compares this NameAndVersion to incoming Name and Version prarameters
        /// </summary>
        /// <param name="ofModelUri">ModelUri of version</param>
        /// <param name="ofPublishDate">Publish Date of NodeSet</param>
        /// <returns></returns>
        public bool HasNameAndVersion(string ofModelUri, DateTime ofPublishDate)
        {
            if (string.IsNullOrEmpty(ofModelUri))
                return false;
            return ModelUri == ofModelUri && PublicationDate >= ofPublishDate;
        }

        public override string ToString()
        {
            string uaStandardIdLabel = UAStandardModelID.HasValue ? $", UA-ID: {UAStandardModelID.Value}" : "";
            return $"{ModelUri} (Version: {ModelVersion}, PubDate: {PublicationDate.ToShortDateString()}{uaStandardIdLabel})";
        }
    }
}
