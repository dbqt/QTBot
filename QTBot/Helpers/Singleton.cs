using Microsoft.Extensions.Logging;

namespace QTBot.Helpers
{
    public class Singleton<T> where T : new()
    {
        #region Singleton
        private static T instance = default(T);

        public static T Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new T();
                }
                return instance;
            }
            private set
            {
                if (instance.Equals(value))
                {
                    Utilities.Log(LogLevel.Information, "Error: Trying to create a second instance of TwitchLibWrapper");
                    return;
                }
                instance = value;
            }
        }
        #endregion Singleton
    }
}
