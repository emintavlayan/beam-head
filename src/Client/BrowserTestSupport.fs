namespace Xunit

open System

/// Marks a shared test when its source is compiled for browser execution.
[<AttributeUsage(AttributeTargets.Method)>]
type FactAttribute() =
    inherit Attribute()

/// Supplies only the assertion operations used by the shared browser test bodies.
type Assert =
    /// Defines the browser-only tolerance for equivalent double-precision values.
    static member private floatTolerance = 0.000000001

    /// Formats floating-point assertion values without exposing binary representation noise.
    static member private formatValue<'T>(value: 'T) =
        match box value with
        | :? float as number -> $"{number:F6}"
        | :? float32 as number -> $"{number:F6}"
        | _ -> string value

    /// Compares floating-point values numerically and all other values structurally.
    static member private valuesEqual<'T when 'T: equality>(expected: 'T, actual: 'T) =
        match box expected, box actual with
        | (:? float as expectedNumber), (:? float as actualNumber) ->
            not (Double.IsNaN expectedNumber || Double.IsNaN actualNumber)
            && abs (expectedNumber - actualNumber) <= Assert.floatTolerance
        | (:? float32 as expectedNumber), (:? float32 as actualNumber) ->
            not (Single.IsNaN expectedNumber || Single.IsNaN actualNumber)
            && abs (expectedNumber - actualNumber) <= float32 Assert.floatTolerance
        | _ -> expected = actual

    /// Checks numerical float equality or structural equality for other values.
    static member Equal<'T when 'T: equality>(expected: 'T, actual: 'T) =
        if not (Assert.valuesEqual (expected, actual)) then
            failwith $"Expected {Assert.formatValue expected}; measured {Assert.formatValue actual}."

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
            failwith $"Measured {Assert.formatValue actual}; expected between {Assert.formatValue minimum} and {Assert.formatValue maximum}."

    /// Checks that an expected result exists in a collection.
    static member Contains<'T>(values: seq<'T>, predicate: 'T -> bool) =
        if not (Seq.exists predicate values) then
            failwith "The expected result was absent."

    /// Applies an assertion to every item.
    static member All<'T>(values: seq<'T>, assertion: 'T -> unit) = Seq.iter assertion values
