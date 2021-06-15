using MongoDB.Bson;
using MongoDB.Driver;
using System.Threading.Tasks;

namespace FinBot.Handlers
{
    class MongoHandler
    {
        public async static Task<BsonDocument> FindById(IMongoCollection<BsonDocument> collection, ulong search_id)
        {
            BsonDocument result = await collection.Find(Builders<BsonDocument>.Filter.Eq("_id", search_id)).FirstOrDefaultAsync();

            if (result == null)
            {
                return null;
            }

            return result;
        }

        public static BsonDocument InsertGuild(ulong guildId)
        {
            BsonDocument document = new BsonDocument { { "_id", (decimal)guildId } };
            MongoClient mongoClient = new MongoClient(Global.Mongoconnstr);
            IMongoDatabase database = mongoClient.GetDatabase("finlay");
            IMongoCollection<BsonDocument> collection = database.GetCollection<BsonDocument>("guilds");
            ForceInsert(collection, document);
            return document;
        }

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
