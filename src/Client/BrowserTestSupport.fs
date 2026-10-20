namespace Xunit

open System

/// Marks a shared test when its source is compiled for browser execution.
[<AttributeUsage(AttributeTargets.Method)>]
type FactAttribute() =
    inherit Attribute()

/// Supplies only the assertion operations used by the shared browser test bodies.
type Assert =
    /// Checks structural equality between the expected and measured values.
    static member Equal<'T when 'T: equality>(expected: 'T, actual: 'T) =
        if expected <> actual then
            failwith $"Expected {expected}; measured {actual}."

    /// Checks an expected true condition.
    static member True(condition: bool) =
        if not condition then
            failwith "An expected condition was false."

    /// Checks an expected false condition.
    static member False(condition: bool) =
        if condition then
            failwith "A rejected condition was accepted."

    /// Checks an inclusive range using comparison semantics that reject nonfinite outliers.
    static member InRange<'T when 'T: comparison>(actual: 'T, minimum: 'T, maximum: 'T) =
        if compare actual minimum < 0 || compare actual maximum > 0 then
            failwith $"Measured {actual}; expected between {minimum} and {maximum}."

    /// Checks that an expected result exists in a collection.
    static member Contains<'T>(values: seq<'T>, predicate: 'T -> bool) =
        if not (Seq.exists predicate values) then
            failwith "The expected result was absent."

    /// Applies an assertion to every item.
    static member All<'T>(values: seq<'T>, assertion: 'T -> unit) = Seq.iter assertion values