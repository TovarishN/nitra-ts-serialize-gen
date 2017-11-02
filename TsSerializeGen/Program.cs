using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace TsSerializeGen
{
    class Program
    {
        private static Dictionary<Type, (Type type, string typeName, short MsgId, IEnumerable<(string name, Type type)> props)> typeDict;

        static (string typeName, string genericParam) GetTypeNaming(FieldInfo fi)
        {
            return (fi.FieldType.Name, fi.FieldType.IsGenericType ? fi.FieldType.GenericTypeArguments[0].Name : "");
        }

        static void Main(string[] args)
        {
            var asm = Assembly.LoadFile(@"D:\work\nitra\nitra\bin\Debug\Stage1\Nitra.ClientServer.Messages.dll");

            var types = asm.GetExportedTypes().Where(x => x.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                                           .FirstOrDefault(a => a.Name == "MsgId" && a.PropertyType == typeof(short)) != null)
                                              .Where(x => !x.IsAbstract)
                            .Select(x =>
                            {
                                var obj = Create(x);
                                if (obj == null)
                                    obj = FormatterServices.GetUninitializedObject(x);
                                var value = x.GetProperty("MsgId").GetValue(obj);
                                var props = x.GetFields(BindingFlags.Instance | BindingFlags.Public)
                                                    .Select(a => (name: a.Name, type: a.FieldType));
                                return (type: x, typeName: x.Name + (x.IsNestedPublic ? $"_{x.DeclaringType.Name}" : ""), MsgId: (short)value, props: props);
                            });

            typeDict = types.ToDictionary(x => x.type);

            var enums = asm.GetExportedTypes()
                .Where(x => x.IsEnum);

            using (var outFile = new StreamWriter(File.Create("NitraMessages.ts")))
            {
                //outFile.WriteLine(string.Join(", ", types.SelectMany(x => x.props).Select(x => x.type).Distinct()));
                outFile.WriteLine(@"
import {SerializeString, SerializeType, SerializeMessage, SerializeInt32, SerializeArr
    , SerializeBoolean, SerializeInt16, SerializeInt64, SerializeUInt32
    , SerializeUInt16, SerializeByte, SerializeFloat, SerializeChar, SerializeDouble} from './serializers';

export interface SolutionId { Value: number; }
export interface ProjectId { Value: number; }
export interface FileId { Value: number; }
export interface FileVersion { Value: number; }
export type FileChange = Insert_FileChange | Delete_FileChange | Replace_FileChange;
export type ObjectDescriptor = Ast_ObjectDescriptor | AstList_ObjectDescriptor | Boolean_ObjectDescriptor | Byte_ObjectDescriptor | Char_ObjectDescriptor
    | Double_ObjectDescriptor | Int16_ObjectDescriptor | Int32_ObjectDescriptor | Int64_ObjectDescriptor
    | NotEvaluated_ObjectDescriptor | Null_ObjectDescriptor | Object_ObjectDescriptor | Parsed_ObjectDescriptor
    | SByte_ObjectDescriptor | Seq_ObjectDescriptor | Single_ObjectDescriptor | String_ObjectDescriptor
    | Symbol_ObjectDescriptor | UInt16_ObjectDescriptor | UInt32_ObjectDescriptor | UInt64_ObjectDescriptor | Unknown_ObjectDescriptor;

export type ContentDescriptor = AstItems_ContentDescriptor | Fail_ContentDescriptor | Items_ContentDescriptor | Members_ContentDescriptor;
export type CompletionElem = Literal_CompletionElem | Symbol_CompletionElem;
export type Message = CheckVersion_ClientMessage | SolutionStartLoading_ClientMessage | SolutionLoaded_ClientMessage | SolutionUnloaded_ClientMessage 
                    | ProjectStartLoading_ClientMessage | ProjectLoaded_ClientMessage | ProjectUnloaded_ClientMessage | ProjectRename_ClientMessage 
                    | ProjectReferenceLoaded_ClientMessage | ProjectReferenceUnloaded_ClientMessage | ReferenceLoaded_ClientMessage | ReferenceUnloaded_ClientMessage 
                    | FileLoaded_ClientMessage | FileReparse_ClientMessage | FileUnloaded_ClientMessage | FileRenamed_ClientMessage | FileInMemoryLoaded_ClientMessage 
                    | FileActivated_ClientMessage | FileDeactivated_ClientMessage | FileChanged_ClientMessage | FileChangedBatch_ClientMessage | PrettyPrint_ClientMessage 
                    | CompleteWord_ClientMessage | CompleteWordDismiss_ClientMessage | FindSymbolReferences_ClientMessage | FindSymbolDefinitions_ClientMessage | ParseTreeReflection_ClientMessage 
                    | GetObjectContent_ClientMessage | GetObjectGraph_ClientMessage | AttachDebugger_ClientMessage | GetLibsMetadata_ClientMessage | GetLibsSyntaxModules_ClientMessage 
                    | GetLibsProjectSupports_ClientMessage | GetFileExtensions_ClientMessage | SetCaretPos_ClientMessage | GetHint_ClientMessage | GetSubHint_ClientMessage 
                    | FindDeclarations_ClientMessage | Shutdown_ClientMessage | FindSymbolDefinitions_ServerMessage | FindSymbolReferences_ServerMessage 
                    | ParseTreeReflection_ServerMessage | ObjectContent_ServerMessage | LibsMetadata_ServerMessage | LibsSyntaxModules_ServerMessage | LibsProjectSupports_ServerMessage 
                    | FileExtensions_ServerMessage | SubHint_ServerMessage | LanguageLoaded_AsyncServerMessage | OutliningCreated_AsyncServerMessage | KeywordsHighlightingCreated_AsyncServerMessage 
                    | MatchedBrackets_AsyncServerMessage | SymbolsHighlightingCreated_AsyncServerMessage | ProjectLoadingMessages_AsyncServerMessage | ParsingMessages_AsyncServerMessage 
                    | MappingMessages_AsyncServerMessage | SemanticAnalysisMessages_AsyncServerMessage | SemanticAnalysisDone_AsyncServerMessage | PrettyPrintCreated_AsyncServerMessage 
                    | ReflectionStructCreated_AsyncServerMessage | RefreshReferencesFailed_AsyncServerMessage | RefreshProjectFailed_AsyncServerMessage | FindSymbolReferences_AsyncServerMessage 
                    | Hint_AsyncServerMessage | Exception_AsyncServerMessage | FoundDeclarations_AsyncServerMessage | CompleteWord_AsyncServerMessage | ProjectSupports | SyntaxModules 
                    | LibMetadata | SymbolRreferences | NSpan | SpanInfo | Insert_FileChange | Delete_FileChange | Replace_FileChange | FileIdentity | FileEntries | Range | Location 
                    | DeclarationInfo | SymbolLocation | CompilerMessage | ProjectSupport | Config | DynamicExtensionInfo | LanguageInfo | SpanClassInfo | OutliningInfo | Literal_CompletionElem 
                    | Symbol_CompletionElem | ReflectionInfo | ParseTreeReflectionStruct | GrammarDescriptor | LibReference | Fail_ContentDescriptor | Members_ContentDescriptor 
                    | Items_ContentDescriptor | AstItems_ContentDescriptor | Unknown_ObjectDescriptor | Null_ObjectDescriptor | NotEvaluated_ObjectDescriptor | Ast_ObjectDescriptor 
                    | Symbol_ObjectDescriptor | Object_ObjectDescriptor | AstList_ObjectDescriptor | Seq_ObjectDescriptor | String_ObjectDescriptor | Int16_ObjectDescriptor 
                    | Int32_ObjectDescriptor | Int64_ObjectDescriptor | Char_ObjectDescriptor | SByte_ObjectDescriptor | UInt16_ObjectDescriptor | UInt32_ObjectDescriptor 
                    | UInt64_ObjectDescriptor | Byte_ObjectDescriptor | Single_ObjectDescriptor | Double_ObjectDescriptor | Boolean_ObjectDescriptor | Parsed_ObjectDescriptor 
                    | PropertyDescriptor | MatchBrackets | VersionedPos;
export enum PrettyPrintState { Disabled,Text, Html }
export enum CompilerMessageSource { ProjectLoading,Parsing, Mapping, SemanticAnalysis }
export enum CompilerMessageType { FatalError,Error, Warning, Hint }
export enum ReflectionKind { Normal,Recovered, Ambiguous, Deleted }
export enum PropertyKind { Simple,DependentIn, DependentOut, DependentInOut, Ast }
");
                outFile.WriteLine($"export type Message = {string.Join(" | ", types.Select(x => x.typeName))};");

                enums.ForEach(x => {
                    outFile.Write($"export enum {x.Name} {{ ");
                    outFile.Write(string.Join(", ", x.GetFields().Where(a => !a.IsInitOnly).Select(a => a.Name)));
                    outFile.Write($" }}\r\n");
                });

                types.ForEach(x =>
                {
                outFile.Write($@"export interface {x.typeName} {{ ");
                    outFile.Write($@"MsgId: {x.MsgId}; ");
                    outFile.Write(string.Join("", x.props.Select(a => $"{a.name}: {GetJsTypeName(a.type)}; ")));
                    outFile.Write(@"}
");
                });


                outFile.Write($@"
export function Serialize(msg: Message): Buffer {{
    switch (msg.MsgId) {{
");
                types.ForEach(x =>
                {
                    outFile.WriteLine($@"case {x.MsgId}: {{ // {x.typeName}");
                    outFile.Write(string.Join("", GetSerializer(x.type)));
                    outFile.Write(@"}
");
                });
                outFile.Write("default: return Buffer.alloc(0);\r\n");
                outFile.Write("}\r\n}\r\n");
            }
        }

        private static string GetSerializer(Type type)
        {
            string GetFun(string pName, Type t)
            {
                if (t == typeof(string))        return $"SerializeString({pName})";
                else if (t == typeof(short))    return $"SerializeInt16({pName})";
                else if (t == typeof(ushort))   return $"SerializeUInt16({pName})";
                else if (t == typeof(int))      return $"SerializeInt32({pName})";
                else if (t == typeof(uint))     return $"SerializeUInt32({pName})";
                else if (t == typeof(float))    return $"SerializeFloat({pName})";
                else if (t == typeof(double))   return $"SerializeDouble({pName})";
                else if (t == typeof(char))     return $"SerializeChar({pName})";
                else if (t == typeof(sbyte) 
                        || t == typeof(byte))   return $"SerializeByte({pName})";

                else if (t == typeof(long) 
                        || t == typeof(ulong))  return $"SerializeInt64({pName})";

                else if (t == typeof(bool))     return $"SerializeBoolean({pName})";

                else if (new[] { "SolutionId"
                                , "FileId"
                                , "ProjectId"
                                , "FileVersion" }
                        .Contains(t.Name))                return $"SerializeInt32({pName}.Value)";

                else if (t.IsEnum)                        return $"SerializeInt32(<number>{pName})";

                else if (t.IsArray 
                    || t.Name == "ImmutableArray`1" 
                    || t.Name == "list`1")
                {
                    var ser = t.IsArray 
                            ? GetFun($"item", t.GetElementType())
                            : typeDict.ContainsKey(t.GenericTypeArguments[0]) 
                                    ? "Serialize(item)"
                                    : GetFun($"item", t.GenericTypeArguments[0]);
                   
                    var ret = $@"SerializeArr({pName}.map(item => {ser}))";
                    return ret;
                }
                else if (typeDict.ContainsKey(t))
                {
                    var ser = string.Join("\r\n,", typeDict[t].props
                                             .Select(a => $"{GetFun($"{pName}.{a.name}", a.type)}"));
                    var res = t.IsValueType 
                                ? $"SerializeType([{ser}])" 
                                : $"SerializeMessage({pName}.MsgId\r\n, [{ser}])";
                    return res;
                    
                }
                else if (new[] { "FileChange", "ObjectDescriptor", "ContentDescriptor", "CompletionElem" }.Contains(t.Name))
                {
                    return $"Serialize({pName})";
                }
                return "Error!!!";
            }

            return $"return {GetFun("msg", type)};\r\n";
        }

        private static object GetJsTypeName(Type type)
        {
            if (type == typeof(string)
                || type == typeof(char))
                return "string";
            if (type == typeof(bool))
                return "boolean";
            else if (type == typeof(int)
                    || type == typeof(short)
                    || type == typeof(ushort)
                    || type == typeof(uint)
                    || type == typeof(long)
                    || type == typeof(ulong)
                    || type == typeof(byte)
                    || type == typeof(double)
                    || type == typeof(float)
                    || type == typeof(sbyte))
                return "number";
            else if (type.Name == "ImmutableArray`1" || type.Name == "list`1")
                return $"{GetJsTypeName(type.GenericTypeArguments[0])}[]";
            else if (type.IsArray)
                return $"{GetJsTypeName(type.GetElementType())}[]";

            return type.Name;
        }

        public static object Create(Type targetType)
        {
            var ctor = targetType.GetConstructors()
                                  .FirstOrDefault();

            if (ctor != null)
            {
                var par = ctor.GetParameters()
                                .Select(b => Create(b.ParameterType))
                                .ToArray();

                var obj = targetType.IsArray
                    ? Activator.CreateInstance(targetType, 0)
                    : ctor.Invoke(par);
                return obj;

            }

            return null;
        }
    }
}
