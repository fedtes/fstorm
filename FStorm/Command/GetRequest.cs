namespace FStorm
{
    public class GetRequest
    {
        /// <summary>
        /// Path to address a collection (of entities), a single entity within a collection, a singleton, as well as a property of an entity.
        /// </summary>
        public string RequestPath { get; set; }
        public GetRequest()
        {
            RequestPath = String.Empty;
        }
    }
}