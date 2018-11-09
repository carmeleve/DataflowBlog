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
            var batchedNumbers = new List<List<int>>();

            var numbers = Enumerable.Range(1, 10).ToList();

            for (var i=0; i < 10; i++)
            {
                batchedNumbers.Add(numbers);
            }

            // We now have a list of 10 lists of the numbers 1 to 10 (stick with me)

            var separateNumbers = new TransformManyBlock<IList<int>, int>(l => l);

            var transformNumbers = new TransformBlock<int, string>(
                n =>
                    {
                        var outputString = $"Number: {n}";

                        return outputString;
                    },
                new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = Environment.ProcessorCount });

            var outputNumbers = new ActionBlock<string>(
                s => { Console.WriteLine(s); },
                new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = Environment.ProcessorCount });

            separateNumbers.LinkTo(transformNumbers, new DataflowLinkOptions { PropagateCompletion = true });
            transformNumbers.LinkTo(outputNumbers, new DataflowLinkOptions { PropagateCompletion = true });

            foreach (List<int> batch in batchedNumbers)
            {
                separateNumbers.Post(batch);
            }

            separateNumbers.Complete();

            
            outputNumbers.Completion.Wait();
        }
    }
}
