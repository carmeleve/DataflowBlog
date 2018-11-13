using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataFlowBlog
{
    using System.Diagnostics;
    using System.Threading.Tasks.Dataflow;

    class Program
    {
        static void Main(string[] args)
        {
            // Create a list of lists, comprising of [1,1,1,1,1], [2,2,2,2,2] etc.
            var batchedNumbers = new List<List<int>>();

            for (var i=0; i < 5; i++)
            {
                batchedNumbers.Add(Enumerable.Repeat(i, 5).ToList());
            }
            
            // Seperates each list it is passed into its individual elements and adds each result to the output
            var separateNumbers = new TransformManyBlock<IList<int>, int>(
                l => l,
                new ExecutionDataflowBlockOptions
                    {
                        MaxDegreeOfParallelism = Environment.ProcessorCount,
                        SingleProducerConstrained = true
                    });
            
            // Takes an int and transforms it into an output string, and adds that string to the output
            var transformNumbers = new TransformBlock<int, string>(
                n =>
                    {
                        var outputString = $"Number: {n}";

                        return outputString;
                    },
                new ExecutionDataflowBlockOptions
                    {
                        MaxDegreeOfParallelism = Environment.ProcessorCount,
                        SingleProducerConstrained = true
                    });

            // Takes a string and writes it out to the console
            var outputNumbers = new ActionBlock<string>(
                s => { Console.WriteLine(s); },
                new ExecutionDataflowBlockOptions
                    {
                        MaxDegreeOfParallelism = Environment.ProcessorCount,
                        SingleProducerConstrained = true
                    });

            // Link the dataflow blocks together
            separateNumbers.LinkTo(transformNumbers, new DataflowLinkOptions { PropagateCompletion = true });
            transformNumbers.LinkTo(outputNumbers, new DataflowLinkOptions { PropagateCompletion = true });

            // Post each batch of numbers to the start of the dataflow
            // Here Post is used because there is no limit to the size of the input buffers, and using SendAsync would involve extra complexity
            foreach (List<int> batch in batchedNumbers)
            {
                separateNumbers.Post(batch);
            }

            // Complete the first block in the chain as the last input data has been sent
            separateNumbers.Complete();

            // Wait for the final block to complete, as the completion is propagated down through the dataflow
            outputNumbers.Completion.Wait();

            Console.ReadKey();
        }
    }
}
