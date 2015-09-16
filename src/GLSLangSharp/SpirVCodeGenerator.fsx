﻿#load "SpirVCore.fs"

open System
open System.Text
open System.IO
open GLSLang.SpirV

do Environment.CurrentDirectory <- __SOURCE_DIRECTORY__

let sb = StringBuilder()
let printfn fmt =
    Printf.kprintf (fun str -> sb.AppendLine str |> ignore; System.Console.WriteLine str) fmt

type Argument =
    | ResultType
    | ResultId

    | Arg of string * Type

type InstructionPrototype = { opCode : OpCode; args : list<Argument> }

let prototypes =
    [
        // Misc
        { opCode = OpCode.Nop; args = [] }
        { opCode = OpCode.Undef; args = [ResultType; ResultId] }

        // Debug
        { opCode = OpCode.Source; args = [Arg("language", typeof<SourceLanguage>); Arg("version", typeof<int>)] }
        { opCode = OpCode.SourceExtension; args = [Arg("extension", typeof<string>)] }
        { opCode = OpCode.Name; args = [Arg("target", typeof<int>)] }
        { opCode = OpCode.MemberName; args = [Arg("_type", typeof<int>); Arg("mem", typeof<int>); Arg("name", typeof<string>)] }
        { opCode = OpCode.String; args = [ResultId; Arg("value", typeof<string>)] }
        { opCode = OpCode.Line; args = [Arg("target", typeof<int>); Arg("file", typeof<int>); Arg("line", typeof<int>); Arg("col", typeof<int>)] }

        // Annotations
        { opCode = OpCode.Decorate; args = [Arg("target", typeof<int>); Arg("decoration", typeof<Decoration>); Arg("args", typeof<int[]>)] }
        { opCode = OpCode.MemberDecorate; args = [Arg("structureType", typeof<int>); Arg("mem", typeof<int>); Arg("decoration", typeof<Decoration>); Arg("args", typeof<int[]>)] }
        { opCode = OpCode.DecorationGroup; args = [ResultId] }
        { opCode = OpCode.GroupDecorate; args = [Arg("decorationGroup", typeof<int>); Arg("targets", typeof<int[]>)] }
        { opCode = OpCode.GroupMemberDecorate; args = [Arg("decorationGroup", typeof<int>); Arg("idLiteralPairs", typeof<int[]>)] }

        // Extensions
        { opCode = OpCode.Extension; args = [Arg("extName", typeof<string>)] }
        { opCode = OpCode.ExtInstImport; args = [ResultId; Arg("name", typeof<string>)] }
        { opCode = OpCode.ExtInst; args = [ResultType; ResultId; Arg("set", typeof<int>); Arg("instruction", typeof<int>); Arg("args", typeof<int[]>)] }

        // Modes
        { opCode = OpCode.MemoryModel; args = [Arg("addressingModel", typeof<AddressingModel>); Arg("memoryModel", typeof<MemoryModel>)] }
        { opCode = OpCode.EntryPoint; args = [Arg("execModel", typeof<ExecutionModel>); Arg("id", typeof<int>); Arg("name", typeof<string>)] }
        { opCode = OpCode.ExecutionMode; args = [Arg("entryPoint", typeof<int>); Arg("mode", typeof<ExecutionMode>); Arg("args", typeof<int[]>)] }
        { opCode = OpCode.Capability; args = [Arg("cap", typeof<Capability>)] }

        // Type-Declarations
        { opCode = OpCode.TypeVoid; args = [ResultId] }
        { opCode = OpCode.TypeBool; args = [ResultId] }
        { opCode = OpCode.TypeInt; args = [ResultId; Arg("width", typeof<int>); Arg("signed", typeof<bool>)] }
        { opCode = OpCode.TypeFloat; args = [ResultId; Arg("width", typeof<int>)] }
        { opCode = OpCode.TypeVector; args = [ResultId; Arg("compType", typeof<int>); Arg("compCount", typeof<int>)] }
        { opCode = OpCode.TypeMatrix; args = [ResultId; Arg("colType", typeof<int>); Arg("colCount", typeof<int>)] }
        { opCode = OpCode.TypeImage; args = [ResultId; Arg("sampledType", typeof<int>); Arg("dim", typeof<int>); Arg("depth", typeof<int>); Arg("arrayed", typeof<bool>); Arg("ms", typeof<bool>); Arg("sampled", typeof<SampleMode>); Arg("format", typeof<ImageFormat>); Arg("access", typeof<int[]>)] }
        { opCode = OpCode.TypeSampler; args = [ResultId] }
        { opCode = OpCode.TypeSampledImage; args = [ResultId; Arg("imageType", typeof<int>)] }
        { opCode = OpCode.TypeArray; args = [ResultId; Arg("elemType", typeof<int>); Arg("length", typeof<int>)] }
        { opCode = OpCode.TypeRuntimeArray; args = [ResultId; Arg("elemType", typeof<int>)] }
        { opCode = OpCode.TypeStruct; args = [ResultId; Arg("fieldTypes", typeof<int[]>)] }
        { opCode = OpCode.TypeOpaque; args = [ResultId; Arg("name", typeof<string>)] }
        { opCode = OpCode.TypePointer; args = [ResultId; Arg("sClass", typeof<StorageClass>); Arg("_type", typeof<int>)] }
        { opCode = OpCode.TypeFunction; args = [ResultId; Arg("retType", typeof<int>); Arg("argTypes", typeof<int[]>)] }
        { opCode = OpCode.TypeEvent; args = [ResultId] }
        { opCode = OpCode.TypeDeviceEvent; args = [ResultId] }
        { opCode = OpCode.TypeReserveId; args = [ResultId] }
        { opCode = OpCode.TypeQueue; args = [ResultId] }
        { opCode = OpCode.TypePipe; args = [ResultId; Arg("_type", typeof<int>); Arg("access", typeof<AccessQualifier>)] }

        // Constants
        { opCode = OpCode.ConstantTrue; args = [ResultType; ResultId] }
        { opCode = OpCode.ConstantFalse; args = [ResultType; ResultId] }
        { opCode = OpCode.Constant; args = [ResultType; ResultId; Arg("values", typeof<int[]>)] }
        { opCode = OpCode.ConstantComposite; args = [ResultType; ResultId; Arg("constituents", typeof<int[]>)] }
        { opCode = OpCode.ConstantSampler; args = [ResultType; ResultId; Arg("addressingMode", typeof<SamplerAddressingMode>); Arg("normalized", typeof<bool>); Arg("filter", typeof<SamplerFilterMode>)] }
        { opCode = OpCode.ConstantNull; args = [ResultType; ResultId] }
            
        // SpecConstants
        { opCode = OpCode.SpecConstantTrue; args = [ResultType; ResultId] }
        { opCode = OpCode.SpecConstantFalse; args = [ResultType; ResultId] }
        { opCode = OpCode.SpecConstant; args = [ResultType; ResultId; Arg("values", typeof<int[]>)] }
        { opCode = OpCode.SpecConstantComposite; args = [ResultType; ResultId; Arg("constituents", typeof<int[]>)] }
        { opCode = OpCode.SpecConstantOp; args = [ResultType; ResultId; Arg("opCode", typeof<OpCode>); Arg("operands", typeof<int[]>)] }

        // Memory
        { opCode = OpCode.Variable; args = [ResultType; ResultId; Arg("storageClas", typeof<StorageClass>); Arg("initializers", typeof<int[]>)] }
        { opCode = OpCode.ImageTexelPointer; args = [ResultType; ResultId; Arg("image", typeof<int>); Arg("coordinate", typeof<Dim>); Arg("sample", typeof<int>)] }
        { opCode = OpCode.Load; args = [ResultType; ResultId; Arg("pointer", typeof<int>); Arg("memoryAccess", typeof<int[]>)] }
        { opCode = OpCode.Store; args = [ResultType; ResultId; Arg("pointer", typeof<int>); Arg("ob", typeof<int>); Arg("memoryAccess", typeof<int[]>)] }
        { opCode = OpCode.CopyMemory; args = [Arg("target", typeof<int>); Arg("source", typeof<int>); Arg("memoryAccess", typeof<int[]>)] }
        { opCode = OpCode.CopyMemorySized; args = [Arg("target", typeof<int>); Arg("source", typeof<int>); Arg("size", typeof<int>); Arg("memoryAccess", typeof<int[]>)] }
        { opCode = OpCode.AccessChain; args = [ResultType; ResultId; Arg("_base", typeof<int>); Arg("indices", typeof<int[]>)] }
        { opCode = OpCode.InBoundsAccessChain; args = [ResultType; ResultId; Arg("_base", typeof<int>); Arg("indices", typeof<int[]>)] }
        { opCode = OpCode.PtrAccessChain; args = [ResultType; ResultId; Arg("_base", typeof<int>); Arg("element", typeof<int>); Arg("indices", typeof<int[]>)] }
        { opCode = OpCode.ArrayLength; args = [ResultType; ResultId; Arg("structure", typeof<int>); Arg("arrMember", typeof<int>)] }
        { opCode = OpCode.GenericPtrMemSemantics; args = [ResultType; ResultId; Arg("ptr", typeof<int>)] }

        // Functions
        { opCode = OpCode.Function; args = [ResultType; ResultId; Arg("ctrl", typeof<FunctionControl>); Arg("_type", typeof<int>)] }
        { opCode = OpCode.FunctionParameter; args = [ResultType; ResultId] }
        { opCode = OpCode.FunctionEnd; args = [] }
        { opCode = OpCode.FunctionCall; args = [ResultType; ResultId; Arg("f", typeof<int>); Arg("args", typeof<int[]>)] }

        // Images


        // Conversion


        // Arithmetic


        // Bit instructions


        // Relations and Logics


        // Derivative instruction


        // Control Flow


        // Atomics


        // Primitive instructions


        // Barrier instructions


        // Group instructions


        // Device-Side enqueue instructions


        // Pipe instructions

    ]



let header() =
    printfn "namespace GLSLang.SpirV"
    printfn ""
    printfn "open System.IO"
    printfn ""

let rec typeName (t : Type) =
    if t = typeof<int> then "uint32"
    elif t = typeof<string> then "string"
    elif t = typeof<bool> then "bool"
    elif t.IsArray then sprintf "%s[]" (typeName (t.GetElementType()))
    else t.Name

let printInstructionType() =
    printfn "type Instruction = "
    for p in prototypes do
        if List.isEmpty p.args then
            printfn "    | %A" p.opCode
        else
            let args = 
                p.args 
                    |> List.map (fun a ->
                        match a with
                            | ResultType -> "resType : uint32"
                            | ResultId -> "resId : uint32"
                            | Arg(n, t) -> sprintf "%s : %s" n (typeName t)
                        )
                    |> String.concat " * "
            printfn "    | %A of %s" p.opCode args

    printfn ""
    printfn ""

let readerString (a : Argument) (offset : byref<int>) =
    match a with
        | ResultId | ResultType -> 
            let res = sprintf "args.UInt32 %d" (offset / 4)
            offset <- offset + 4
            res
        | Arg(name, t) ->
            if t = typeof<string> then
                sprintf "args.String %d" offset
            elif t = typeof<int> then
                let res = sprintf "args.UInt32 %d" (offset / 4)
                offset <- offset + 4
                res
            elif t.IsEnum then
                let res = sprintf "args.UInt32 %d |> unbox<%s>" (offset / 4) t.Name
                offset <- offset + 4
                res
            elif t = typeof<bool> then
                let res = sprintf "args.UInt32 %d = 1u" (offset / 4)
                offset <- offset + 4
                res
            elif t.IsArray && t.GetElementType() = typeof<int> then
                sprintf "args.UInt32Array %d" (offset / 4)
            else
                failwithf "cannot read: %A" t

let readers() =
    printfn "module SpirVReader = "
    printfn "    let private ofRawInstruction (i : RawInstruction) = "
    printfn "        let args = i.operands"
    printfn "        match i.opCode with"
    for p in prototypes do
        if List.isEmpty p.args then
            printfn "            | OpCode.%A -> %A" p.opCode p.opCode
        else
            let readers = 
                [
                    let offset = ref 0
                    for a in p.args do
                        let str = readerString a &offset.contents 
                        yield str

                ] |> String.concat ", "

            printfn "            | OpCode.%A -> %A(%s)" p.opCode p.opCode readers
    printfn "            | code -> failwithf \"unknown OpCode: %%A\" code"
    printfn ""
    printfn "    let readStream (i : Stream) = "
    printfn "        let m = RawReader.read i"
    printfn "        m.instructions |> List.map ofRawInstruction"
    printfn ""
    printfn ""

let writers() =
    printfn "module SpirVWriter = "
    printfn "    let private toRawInstruction (i : Instruction) = "
    printfn "        match i with"
    for p in prototypes do
        if List.isEmpty p.args then
            printfn "            | %A -> { opCode = OpCode.%A; operands = RawOperands() }" p.opCode p.opCode
        else
            let args = 
                p.args 
                    |> List.map (fun a ->
                        match a with
                            | ResultType -> "resType"
                            | ResultId -> "resId"
                            | Arg(name, _) -> name
                       )
                    |> String.concat ", "

            printfn "            | %A(%s) -> { opCode = OpCode.%A; operands = RawOperands(%s) }" p.opCode args p.opCode args

    printfn ""
    printfn "    let writeStream (o : Stream) (instructions : list<Instruction>) = "
    printfn "        let raw = instructions |> List.map toRawInstruction"
    printfn "        RawWriter.write o raw"
    printfn ""
    printfn ""

let generate() =
    header()
    printInstructionType()
    readers()
    writers()

    let content = sb.ToString()
    File.WriteAllText("SpirV.fs", content)