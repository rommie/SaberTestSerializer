using System.IO;
using System.Threading.Tasks;
using SerializerTests.Nodes;

namespace SerializerTests.Interfaces
{
    //Interface can't be modified.
    public interface IListSerializer
    {

        /// <summary>
        /// Serializes all nodes in the list, including topology of the Random links, into stream.
        /// </summary>
        Task Serialize(ListNode head, Stream s);

        /// <summary>
        /// Deserializes the list from the stream, returns the head node of the list.
        /// </summary>
		/// <exception cref="System.ArgumentException">Thrown when a stream has invalid data.</exception>
        Task<ListNode> Deserialize(Stream s);

        /// <summary>
        /// Makes a deep copy of the list, returns the head node of the list.
        /// </summary>
        Task<ListNode> DeepCopy(ListNode head);

    }
}
