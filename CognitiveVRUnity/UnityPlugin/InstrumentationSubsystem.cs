using System.Collections.Generic;

namespace CognitiveVR
{
    public class TransactionSnapshot
    {
        public string category;
        public Dictionary<string, object> properties;
        public float[] position = new float[3] { 0, 0, 0 };
        public double timestamp;

        public TransactionSnapshot(string cat, Dictionary<string, object> props, float[] pos, double time)
        {
            category = cat;
            properties = props;
            position = pos;
            timestamp = time;
        }
    }

    public static class InstrumentationSubsystem
    {
        public static List<TransactionSnapshot> CachedTransactions = new List<TransactionSnapshot>();
        public static void init()
        {
            Util.cacheCurrencyInfo();
        }

        public static void beginTransaction(string category, string timeoutMode, double timeout, string transactionId, Dictionary<string, object> properties)
        {
            new CoreSubsystem.DataPointBuilder("datacollector_beginTransaction")
            .setArg(category)
            .setArg(timeoutMode)
            .setArg(timeout)
            .setArg(transactionId)
            .setArg(properties)
            .send();
        }

        public static void updateTransaction(string category, int progress, string transactionId, Dictionary<string, object> properties)
        {
            new CoreSubsystem.DataPointBuilder("datacollector_updateTransaction")
            .setArg(category)
            .setArg(progress)
            .setArg(transactionId)
            .setArg(properties)
            .send();
        }

        public static void endTransaction(string category, string result, string transactionId, Dictionary<string, object> properties, float[] position)
        {
            CachedTransactions.Add(new TransactionSnapshot(category, properties, position, Util.Timestamp()));

            new CoreSubsystem.DataPointBuilder("datacollector_endTransaction")
            .setArg(category)
            .setArg(result)
            .setArg(transactionId)
            .setArg(properties)
            .send();
        }

        /**
         * Updates state information about the user.
         *
         * @param properties A key-value object representing the user state we want to update. This can be a nested object structure.
         */
        public static void updateUserState(IDictionary<string, object> properties)
        {
            new CoreSubsystem.DataPointBuilder("datacollector_updateUserState").setArg(properties).send();
        }

        /**
         * Updates state information about a device.
         *
         * @param properties A key-value object representing the device state we want to update. This can be a nested object structure.
         */
        public static void updateDeviceState(IDictionary<string, object> properties)
        {
            new CoreSubsystem.DataPointBuilder("datacollector_updateDeviceState").setArg(properties).send();
        }

        /**
         * Update a collection balance for a user.
         *
         * @param name                  The name of the collection.
         * @param balance               The new balance of the collection.
         * @param balanceModification   The change in balance being recorded.  To reduce the balance, specify a negative number.
         * @param isCurrency            Whether or not this collection represents a currency in the application.
         */
        public static void updateCollection(string name, double balance, double balanceModification, bool isCurrency)
        {
            new CoreSubsystem.DataPointBuilder("datacollector_updateCollection")
            .setArg(name)
            .setArg(balance)
            .setArg(balanceModification)
            .setArg(isCurrency)
            .send();
        }
    }
}
