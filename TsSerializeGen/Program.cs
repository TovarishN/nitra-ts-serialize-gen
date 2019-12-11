using System;
using System.Collections.Generic;
using System.Configuration;
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

        static void GenerateSerializer()
        {
            var dllPath = ConfigurationSettings.AppSettings["MessageDllPath"];
            var asm = Assembly.LoadFrom(dllPath);

            var types = asm.GetExportedTypes().Where(x => x.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                                           .FirstOrDefault(a => a.Name == "MsgId" && a.PropertyType == typeof(short)) != null)
                                              .Where(x => !x.IsAbstract)
                            .Select(x =>
                            {
                                var obj = Create(x);
                                if (obj == null)
                                    obj = FormatterServices.GetUninitializedObject(x);
                                var value = x.GetProperty("MsgId").GetValue(obj);
                                var props = x.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
                                                    .Select(a => (name: a.Name, type: a.FieldType));
                                return (type: x, typeName: x.Name + (x.IsNestedPublic ? $"_{x.DeclaringType.Name}" : ""), MsgId: (short)value, props: props);
                            });

            typeDict = types.ToDictionary(x => x.type);

            var enums = asm.GetExportedTypes()
                .Where(x => x.IsEnum);

            using (var outFile = new StreamWriter(File.Create("C:\\work\\nitra-vscode\\src\\nitra\\pipe\\NitraMessages.ts")))
            {
                outFile.Write(@"export interface SolutionId { Value: number; }
export type ProjectId = { Value: number; }
export type FileId = { Value: number; }
export type FileVersion = { Value: number; }
export type FileChange = Insert_FileChange | Delete_FileChange | Replace_FileChange;
export type ObjectDescriptor = Ast_ObjectDescriptor | AstList_ObjectDescriptor | Boolean_ObjectDescriptor | Byte_ObjectDescriptor | Char_ObjectDescriptor
    | Double_ObjectDescriptor | Int16_ObjectDescriptor | Int32_ObjectDescriptor | Int64_ObjectDescriptor
    | NotEvaluated_ObjectDescriptor | Null_ObjectDescriptor | Object_ObjectDescriptor | Parsed_ObjectDescriptor
    | SByte_ObjectDescriptor | Seq_ObjectDescriptor | Single_ObjectDescriptor | String_ObjectDescriptor
    | Symbol_ObjectDescriptor | UInt16_ObjectDescriptor | UInt32_ObjectDescriptor | UInt64_ObjectDescriptor | Unknown_ObjectDescriptor;

export type ContentDescriptor = AstItems_ContentDescriptor | Fail_ContentDescriptor | Items_ContentDescriptor | Members_ContentDescriptor;
export type CompletionElem = Literal_CompletionElem | Symbol_CompletionElem;
");

                outFile.WriteLine($@"export type Message = {string.Join("\r\n| ", types.Buffer(4)
                                                                                    .Select(x => string.Join(" | ", x.Select(a => a.typeName))))};");

                enums.ForEach(x =>
                {
                    outFile.Write($"export enum {x.Name} {{ ");
                    outFile.Write(string.Join(", ", x.GetFields().Where(a => !a.IsInitOnly).Select(a => a.Name)));
                    outFile.Write($" }}\r\n");
                });

                
                //var enumStr = string.Join("\r\n, ", types.Select(x => $"{x.typeName} = {x.MsgId}"));
                //outFile.Write($"export enum MsgEnum {{ {enumStr} }};");

                types.ForEach(x =>
                {
                    //outFile.Write($@"export type {x.typeName}MsgId = {x.MsgId};");
                    outFile.Write($@"export interface {x.typeName} {{ ");
                    outFile.Write($@"MsgId: {x.MsgId}; ");
                    outFile.Write(string.Join("", x.type.BaseType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
                                                             .Select(a => (name: a.Name, type: a.FieldType))
                                                    .Concat(x.props).Select(a => $"{a.name}: {GetJsTypeName(a.type)}; ")));
                    outFile.Write(@"}
");
                });

            }

            using (var serFile = new StreamWriter(File.Create("C:\\work\\nitra-vscode\\src\\nitra\\pipe\\NitraSerialize.ts")))
            {

                serFile.Write(@"import {Message} from './NitraMessages';
import {SerializeString, SerializeType, SerializeMessage, SerializeInt32, SerializeArr
    , SerializeBoolean, SerializeInt16, SerializeInt64, SerializeUInt32
    , SerializeUInt16, SerializeByte, SerializeFloat, SerializeChar, SerializeDouble} from './serializers';
");

                serFile.Write($@"
export function Serialize(msg: Message): Buffer {{
    switch (msg.MsgId) {{
");
                types.ForEach(x =>
                {
                    serFile.WriteLine($@"case {x.MsgId}: {{ // {x.typeName}");
                    serFile.Write(string.Join("", GetSerializer(x.type)));
                    serFile.Write(@"}
");
                });
                serFile.Write("default: return Buffer.alloc(0);\r\n");
                serFile.Write("}\r\n}\r\n");
            }
        }

        static void GenerateDeserializer()
        {
            var dllPath = ConfigurationSettings.AppSettings["MessageDllPath"];
            var asm = Assembly.LoadFile(dllPath);

            var types = asm.GetExportedTypes().Where(x => x.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                                                           .FirstOrDefault(a => a.Name == "MsgId" && a.PropertyType == typeof(short)) != null)
                                              .Where(x => !x.IsAbstract)
                            .Select(x =>
                            {
                                var obj = Create(x);
                                if (obj == null)
                                    obj = FormatterServices.GetUninitializedObject(x);
                                var value = x.GetProperty("MsgId").GetValue(obj);
                                var props = x.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
                                                    .Select(a => (name: a.Name, type: a.FieldType));

                                return (type: x, typeName: x.Name + (x.IsNestedPublic ? $"_{x.DeclaringType.Name}" : ""), MsgId: (short)value, props: props);
                            });

            typeDict = types.ToDictionary(x => x.type);

            var enums = asm.GetExportedTypes()
                .Where(x => x.IsEnum);

            using (var deSerFile = new StreamWriter(File.Create("C:\\work\\nitra-vscode\\src\\nitra\\pipe\\NitraDeserialize.ts")))
            {

                deSerFile.Write(@"import * as Msg from './NitraMessages';
import Int64 = require('node-int64');
import { PushStringArrayDeserializer, cast
        , PushObjectDescriptorArrayDeserializer, PushFileChangeArrayDeserializer, PushStringDeserializer, PushCompletionElemArrayDeserializer } from './deserializers';
import { FunPile } from './FunPile';
");

                deSerFile.Write($@"
export function PushDeserializer(msg: Msg.Message, pile: FunPile): void {{
	switch (msg.MsgId) {{
");
                types.ForEach(x =>
                {
                    deSerFile.WriteLine($@"case {x.MsgId}: {{ // {x.typeName}");
                    deSerFile.Write(string.Join("", GetDeSerializer(x.type)));
                    deSerFile.Write(@"break;}
");
                });
                deSerFile.Write("}\r\n}\r\n");
            }
        }
        static void Main(string[] args)
        {
            GenerateSerializer();
            GenerateDeserializer();
        }

        private static string GetSerializer(Type type)
        {
            string GetFun(string pName, Type t)
            {
                if (t == typeof(string)) return $"SerializeString({pName})";
                else if (t == typeof(short)) return $"SerializeInt16({pName})";
                else if (t == typeof(ushort)) return $"SerializeUInt16({pName})";
                else if (t == typeof(int)) return $"SerializeInt32({pName})";
                else if (t == typeof(uint)) return $"SerializeUInt32({pName})";
                else if (t == typeof(float)) return $"SerializeFloat({pName})";
                else if (t == typeof(double)) return $"SerializeDouble({pName})";
                else if (t == typeof(char)) return $"SerializeChar({pName})";
                else if (t == typeof(sbyte)
                        || t == typeof(byte)) return $"SerializeByte({pName})";

                else if (t == typeof(long)
                        || t == typeof(ulong)) return $"SerializeInt64({pName})";

                else if (t == typeof(bool)) return $"SerializeBoolean({pName})";

                else if (new[] { "SolutionId"
                                , "FileId"
                                , "ProjectId"
                                , "FileVersion" }
                        .Contains(t.Name)) return $"SerializeInt32({pName}.Value)";

                else if (t.IsEnum) return $"SerializeInt32(<number>{pName})";

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
                    var baseProps = t.BaseType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
                                                    .Select(a => (name: a.Name, type: a.FieldType));

                    var ser = string.Join("\r\n,", baseProps.Concat(typeDict[t].props)//.Distinct()
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

        private static string GetDeSerializer(Type type)
        {
            string GetFun(string pName, Type t)
            {
				if (t == typeof(string)) return $"pile.curPush((buf, pile) => PushStringDeserializer(buf, pile, (str:string) => {{ {pName} = str; }}, () => {{ return {pName}; }})); ";
				else if (t == typeof(short)) return $"pile.curPush((buf, pile) => {{ {pName} = buf.readInt16LE(0); }});";
				else if (t == typeof(ushort)) return $"pile.curPush((buf, pile) => {{ {pName} = buf.readInt16LE(0); }});";
				else if (t == typeof(int)) return $"pile.curPush((buf, pile) => {{ {pName} = buf.readInt32LE(0); }});";
				else if (t == typeof(uint)) return $"pile.curPush((buf, pile) => {{ {pName} = buf.readInt32LE(0); }});";
				else if (t == typeof(float)) return $"pile.curPush((buf, pile) => {{ {pName} = buf.readFloatLE(0); }});";
				else if (t == typeof(double)) return $"pile.curPush((buf, pile) => {{ {pName} = buf.readDoubleLE(0); }});";
				else if (t == typeof(char)) return $"pile.curPush((buf, pile) => {{ {pName} = buf.toString(); }});";
				else if (t == typeof(sbyte)
						|| t == typeof(byte)) return $"pile.curPush((buf, pile) => {{ {pName} = buf.readUInt8(0); }});";

				else if (t == typeof(long)
						|| t == typeof(ulong)) return $"pile.curPush((buf, pile) => {{ {pName} = new Int64(buf).valueOf(); }});";

				else if (t == typeof(bool)) return $"pile.curPush((buf, pile) => {{ {pName} = buf.readUInt8(0) === 1; }});";

				else if (new[] { "SolutionId"
								, "FileId"
								, "ProjectId"
								, "FileVersion" }
						.Contains(t.Name)) return $"pile.curPush((buf, pile) => {{ {pName} = {{ Value: buf.readInt32LE(0) }}; }});";

				else if (t.IsEnum)
				{
					var ut = Enum.GetUnderlyingType(t);
					var funName = ut.Name == "Byte" ? "readInt8" : "readInt32LE";
					//if(t.BaseType)
					return $"pile.curPush((buf, pile) => {{ {pName} = <Msg.{t.Name}>buf.{funName}(0); }});";
				}

				else if (t.IsArray
					|| t.Name == "ImmutableArray`1"
					|| t.Name == "list`1")
				{
					var arrType = t.IsArray ? t.GetElementType() : t.GenericTypeArguments[0];

					if (typeDict.ContainsKey(arrType))
					{
						var ret = $@"
pile.curPush((buf,pile) => {{
	let length = buf.readInt32LE(0);
	{pName} = [];
    if(length > 0) pile.raise();
	for (let i = 0; i < length; i++) {{
		{pName}.push(<Msg.{typeDict[arrType].typeName}>{{ MsgId: {typeDict[arrType].MsgId} }});
		PushDeserializer({pName}[i], pile);
	}}
}});
";
						return ret;
					}
					else if (new[] { "FileChange", "ObjectDescriptor", "ContentDescriptor", "CompletionElem" }.Contains(arrType.Name))
					{
						var ret = $@"
pile.curPush((buf,pile) => {{
	let length = buf.readInt32LE(0);
    if(length > 0) pile.raise();
	{pName} = [];
		Push{arrType.Name}ArrayDeserializer({pName}, length, pile);
}});
";
						return ret;
					}
					else
					{
						var ret = $@"
pile.curPush((buf,pile) => {{
	let length = buf.readInt32LE(0);
	{pName} = [];
    if(length > 0) pile.raise();
	for (let i = 0; i < length; i++) {{
		PushStringArrayDeserializer({pName}, i, pile);
	}}
}});
";
						return ret;
					}

				}
				else if (typeDict.ContainsKey(t))
				{
					var baseProps = t.BaseType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
												   .Select(a => (name: a.Name, type: a.FieldType));

					var ser = string.Join("\r\n", baseProps.Concat(typeDict[t].props)
											 .Select(a =>
											 {
												 var ret = typeDict.ContainsKey(a.type)
													 ? $"{pName}.{a.name} = cast<Msg.{typeDict[a.type].typeName}>(<Msg.Message>{{ MsgId: {typeDict[a.type].MsgId} }}); PushDeserializer({pName}.{a.name}, pile);"
													 : $"{GetFun($"{pName}.{a.name}", a.type)}";
												 //$"{GetFun($"{pName}.{a.name}", a.type)}";

												 //var ret = $"retStack.push(...GetDeserializer({pName}.{a.name}))";
												 return ret;
											 }));
					return ser;
				}
				else if (new[] { "FileChange", "ObjectDescriptor", "ContentDescriptor", "CompletionElem" }.Contains(t.Name))
				{
					return $"PushDeserializer({pName}, pile);";
				}
                return "Error!!!";
            }

            return $"{GetFun("msg", type)}\r\n";
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
