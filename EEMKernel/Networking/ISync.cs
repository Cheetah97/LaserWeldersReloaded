namespace EemRdx.Networking
{
    public interface ISync<T>
    {
        T Data { get; }
        string DataDescription { get; }

        /// <summary>
        /// Asks the server for actual value.
        /// </summary>
        void Ask();
        /// <summary>
        /// Updates a variable or sends a request to server (if called clientside).
        /// </summary>
        void Set(T New);
    }

    public interface IRegistrableSync<T> : ISync<T>
    {
        /// <summary>
        /// Registers a handler. Don't forget to initialize Networker beforehand.
        /// </summary>
        void Register();
        /// <summary>
        /// Unregisters a handler.
        /// </summary>
        void Unregister();
    }
}