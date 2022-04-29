using SerializerTests.Nodes;

namespace SerializerTests
{
    public static class ListNodeExtension
    {
        public static ListNode MoveNext(this ListNode node, int steps)
        {
            while (node != null && steps != 0)
            {
                if (steps > 0)
                {
                    node = node.Next;
                    steps--;
                }
                else
                {
                    node = node.Previous;
                    steps++;
                }
            }
            return node;
        }

        public static ListNode AddNode(this ListNode tail, string data)
        {
            tail.Next = new ListNode
            {
                Previous = tail,
                Data = data
            };
            return tail.Next;
        }

    }
}
