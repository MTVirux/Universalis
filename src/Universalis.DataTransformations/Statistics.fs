﻿namespace Universalis.DataTransformations

[<AbstractClass; Sealed>]
type Statistics =
    /// <summary>
    /// Calculates the population standard deviation of the provided sequence of numbers.
    /// </summary>
    /// <param name="numbers">The population to calculate the standard deviation over.</param>
    static member PopulationStd(numbers: seq<single>) =
        let average = Seq.average numbers
        let length = Seq.length numbers
        sqrt ((Seq.fold (fun agg next -> agg + (single next - average) ** 2.0f) 0.0f numbers) / single length)

    /// <summary>
    /// Groups the provided sequence of numbers into a dictionary of numbers and the number
    /// of times each number appears in the sequence.
    /// </summary>
    /// <param name="numbers">The sequence of numbers to group.</param>
    static member GetDistribution(numbers: seq<int>) =
        dict (Seq.countBy (fun n -> n) numbers)