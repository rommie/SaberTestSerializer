using System;
using System.Text;
using System.Text.Json;
using System.IO.Pipelines;
using SerializerTests.Interfaces;
using SerializerTests.Nodes;

namespace SerializerTests.Implementations
{   

    //Specify your class\file name and complete implementation.
    public class RomanCherepanovSerializer : IListSerializer
    {    
        // Json property names stored as byte array to avoid additional allocation
        private static readonly byte[] BytesId =
            Encoding.UTF8.GetBytes("id");

        private static readonly byte[] BytesData =
            Encoding.UTF8.GetBytes("data");

        private static readonly byte[] BytesRef =
            Encoding.UTF8.GetBytes("ref");

        //the constructor with no parameters is required and no other constructors can be used.
        public RomanCherepanovSerializer()
        {
            //...
        }

        /// <inheritdoc/>
        public Task<ListNode> DeepCopy(ListNode head)
        {
            if (head is null)
            {
                throw new ArgumentNullException(nameof(head));
            }

            var nodesMap = new Dictionary<ListNode, ListNode>();
            ListNode newHead = new ListNode { Data = head.Data };
            nodesMap.Add(head, newHead);

            var newTail = newHead;
            var currentSourceNode = head.Next;
            while (currentSourceNode != null)
            {
                var copiedNode = new ListNode()
                {
                    Data = currentSourceNode.Data,
                    Previous = newTail,
                    Random = currentSourceNode.Random
                };
                newTail.Next = copiedNode;
                newTail = copiedNode;

                nodesMap.Add(currentSourceNode, copiedNode);

                currentSourceNode = currentSourceNode.Next;
            }

            currentSourceNode = newHead;
            while (currentSourceNode != null)
            {
                if (currentSourceNode.Random != null)
                {
                    currentSourceNode.Random = nodesMap[currentSourceNode.Random];
                }

                currentSourceNode = currentSourceNode.Next;
            }

            return Task.FromResult(newHead);
        }

        /// <inheritdoc/>
        public async Task Serialize(ListNode head, Stream s)
        {
            if (head is null)
            {
                throw new ArgumentNullException(nameof(head));
            }

            if (s is null)
            {
                throw new ArgumentNullException(nameof(s));
            }

            var indexesMap = new Dictionary<ListNode, int>();
            var current = head;

            while (current != null)
            {
                indexesMap.Add(current, indexesMap.Count);
                current = current.Next;
            }

            await using var writer = new Utf8JsonWriter(s);

            writer.WriteStartArray();
            foreach (var node in indexesMap)
            {
                writer.WriteStartObject();
                writer.WriteNumber(BytesId, node.Value);
                if (node.Key.Random is not null)
                {
                    writer.WriteNumber(BytesRef, indexesMap[node.Key.Random]);
                }
                if (node.Key.Data is not null)
                {
                    writer.WriteString(BytesData, node.Key.Data);
                }
                writer.WriteEndObject();
                await writer.FlushAsync().ConfigureAwait(false);
            }
            writer.WriteEndArray();
            await writer.FlushAsync();
        }

        /// <inheritdoc/>
        public async Task<ListNode> Deserialize(Stream s)
        {
            if (s is null) 
            { 
                throw new ArgumentNullException(nameof(s));
            }

            var reader = PipeReader.Create(s, new StreamPipeReaderOptions(leaveOpen: true));
            
            // Storage for founded nodes. It uses for recreating references between nodes
            var nodesMap = new Dictionary<int, (ListNode Node, int? Random)>();

            bool inArray = false;
            bool inObject = false;
            bool inIdProperty = false;
            bool inRefProperty = false;
            bool inDataProperty = false;

            int nodeId = default;
            int? nodeRandom = null;
            string nodeData = null;

            JsonReaderState jsonState = default;
            long lastOffset = 0;
            while (true)
            {
                var result = await reader.ReadAsync().ConfigureAwait(false);
                
                try
                {
                    // We use local function because we can't use ref struct in async method
                    jsonState = ProcessBuffer(result, jsonState, out var position);

                    reader.AdvanceTo(position, result.Buffer.End);
                } 
                catch (JsonException ex)
                {
                    // We have to hide original exception because of IListSerializer description
                    throw new ArgumentException("Invalid JSON in stream", nameof(s), ex);
                } 
                if (result.IsCompleted)
                    break;
            }

            if (inArray)
                throw new ArgumentException("Invalid end of stream", nameof(s));

            if (nodesMap.Count == 0)
                return null;

            if (!nodesMap.ContainsKey(0))
            {
                throw new ArgumentException("Cannot find head of list in stream", nameof(s)); 
            }

            ListNode previousNode = null;
            // We assume that the order of nodes might be changed
            foreach (var pair in nodesMap.OrderBy(x => x.Key))
            {
                var (node, random) = pair.Value;
                node.Previous = previousNode;
                if (random.HasValue)
                {
                    if (nodesMap.ContainsKey(random.Value))
                    {
                        node.Random = nodesMap[random.Value].Node;
                    }
                    else
                    {                        
                        throw new ArgumentException("Cannot find reference for list item", nameof(s));
                    }
                }

                if (previousNode != null)
                {
                    previousNode.Next = node;
                }

                previousNode = node;
            }

            return nodesMap[0].Node;

            JsonReaderState ProcessBuffer(
                in ReadResult result,
                in JsonReaderState jsonReaderState,
                out SequencePosition position)
            {
                var reader = new Utf8JsonReader(result.Buffer, result.IsCompleted, jsonReaderState);

                while (reader.Read())
                {
                    if (inIdProperty)
                    {
                        if (reader.TokenType != JsonTokenType.Number || !reader.TryGetInt32(out int id))
                        {
                            throw new ArgumentException("Invalid 'id' value", nameof(s));
                        }
                        nodeId = id;
                        inIdProperty = false;
                    }
                    else if (inRefProperty)
                    {
                        if (reader.TokenType != JsonTokenType.Number || !reader.TryGetInt32(out int rand))
                        {
                            throw new ArgumentException("Invalid 'ref' value", nameof(s));
                        }
                        nodeRandom = rand;
                        inRefProperty = false;
                    }
                    else if (inDataProperty)
                    {
                        if (reader.TokenType != JsonTokenType.String)
                        {
                            throw new ArgumentException("Invalid 'data' value", nameof(s));
                        }
                        nodeData = reader.GetString();
                        inDataProperty = false;
                    }

                    // Changing current processing state by token type
                    // I guess it can be improved through soke kind of state machine or smth
                    switch (reader.TokenType)
                    {
                        case JsonTokenType.StartArray:
                            if (inArray)
                            {
                                throw new ArgumentException("Nested arrays are unsupported", nameof(s));
                            }
                            inArray = true;
                            break;
                        case JsonTokenType.EndArray:
                            inArray = false;
                            break;
                        case JsonTokenType.StartObject:
                            if (inObject)
                            {
                                throw new ArgumentException("Nested objects are unsupported", nameof(s));
                            }
                            inObject = true;
                            break;
                        case JsonTokenType.EndObject:
                            // If we've reached the end of current object we'll add new node
                            AddNode();
                            inObject = inIdProperty = inRefProperty = inDataProperty = false;
                            break;
                        case JsonTokenType.PropertyName:
                            if (reader.ValueSpan.SequenceEqual(BytesId))
                            {
                                inIdProperty = true;
                            }
                            else if (reader.ValueSpan.SequenceEqual(BytesRef))
                            {
                                inRefProperty = true;
                            }
                            else if (reader.ValueSpan.SequenceEqual(BytesData))
                            {
                                inDataProperty = true;
                            }
                            break;
                    }
                }

                position = reader.Position;
                return reader.CurrentState;
            }

            void AddNode()
            {
                if (!nodesMap.TryAdd(nodeId, (new ListNode { Data = nodeData }, nodeRandom)))
                {
                    throw new ArgumentException("Invalid list item in stream", nameof(s));
                }

                nodeData = null;
                nodeRandom = null;
            }
        }
    }
}
