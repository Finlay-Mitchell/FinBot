using MongoDB.Bson;
using MongoDB.Driver;
using System.Threading.Tasks;

namespace FinBot.Handlers
{
    class MongoHandler
    {
        /// <summary>
        /// Searches the collection for the _id value.
        /// </summary>
        /// <param name="collection">The collection to search through.</param>
        /// <param name="search_id">The id to search for.</param>
        public async static Task<BsonDocument> FindById(IMongoCollection<BsonDocument> collection, ulong search_id)
        {
            BsonDocument result = await collection.Find(Builders<BsonDocument>.Filter.Eq("_id", search_id)).FirstOrDefaultAsync();

            if (result == null)
            {
                return null;
            }

            return result;
        }

        /// <summary>
        /// Inserts a guild into the database to allow configuration.
        /// </summary>
        /// <param name="guildId">The id of the guild to add to the database.</param>
        /// <returns></returns>
        public static BsonDocument InsertGuild(ulong guildId)
        {
            BsonDocument document = new BsonDocument { { "_id", (decimal)guildId } };
            MongoClient mongoClient = new MongoClient(Global.Mongoconnstr);
            IMongoDatabase database = mongoClient.GetDatabase("finlay");
            IMongoCollection<BsonDocument> collection = database.GetCollection<BsonDocument>("guilds");
            ForceInsert(collection, document);
            return document;
        }

        /// <summary>
        /// Writes a document to the MongoDB database.
        /// </summary>
        /// <param name="collection">The collection to insert into.</param>
        /// <param name="document">The document to write.</param>
        public async static void ForceInsert(IMongoCollection<BsonDocument> collection, BsonDocument document)
        {
            if (document.Contains("_id"))
            {
                await collection.UpdateOneAsync(Builders<BsonDocument>.Filter.Eq("_id", document.GetValue("_id")), Builders<BsonDocument>.Update.Set("$set", document));
            }

            else
            {
                await collection.InsertOneAsync(document);
            }
        }
    }
}
