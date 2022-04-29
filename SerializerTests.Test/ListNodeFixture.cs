using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SerializerTests.Nodes;

namespace SerializerTests.Test
{
    public class ListNodeFixture
    {
        private readonly string DataSimpleString = "Simple sample string";
        private readonly string DataMultilineString = "Multiline" + Environment.NewLine + "string";
        private readonly string DataCyrillicString = "Кириллица";
        private readonly string DataSpecialSymbolString = "{Some text;;;\t:\t}";        
            
        public ListNode SampleList { get; }

        public ListNodeFixture()
        {
            SampleList = new ListNode() 
            { 
                Data = "Head",
                Next = new ListNode()
            };

            var node = SampleList
                .AddNode(DataSimpleString)
                .AddNode(DataCyrillicString)
                .AddNode(DataSpecialSymbolString)
                .AddNode(DataMultilineString)
                .AddNode(null)
                .AddNode("Tail");

            SampleList.Next.Next.Random = node;
            node.Previous.Random = SampleList.Next;

        }

        public ListNode GenerateList(int nodesCount = 10000, bool useLargeStrings = false)
        {
            if (nodesCount == 0)
                return null;

            var DataSampleLongString = new Lazy<string>(() => new string('X', 1024 * 1024));

            var head = new ListNode()
            {
                Data = "Head",
                Next = new ListNode()
            };

            ListNode tail = head;

            var i = 0;
            var stringTypesCount = useLargeStrings ? 4 : 5;
            while (i < nodesCount)
            {
                for (int stringType = 0; stringType < stringTypesCount && i < nodesCount; stringType++)
                {

                    tail = stringType switch
                    {
                        0 => tail.AddNode(DataMultilineString),
                        1 => tail.AddNode(DataSimpleString),
                        2 => tail.AddNode(DataCyrillicString),
                        3 => tail.AddNode(DataSpecialSymbolString),
                        4 => tail.AddNode(DataSampleLongString.Value),
                        _ => null
                    };

                    i++;
                }
            }


            ListNode p1 = head.MoveNext(3);
            ListNode p2 = tail.MoveNext(-3);

            while (p1 != null && p2 != null)
            {
                p1.Random = p2;
                p1 = p1.MoveNext(3);
                p2 = p2.MoveNext(-3);
            }

            return head;
        }
    }
}
