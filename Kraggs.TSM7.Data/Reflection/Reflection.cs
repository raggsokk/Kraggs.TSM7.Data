#region License
/*
    TSM 7.1 Utility library.
    Copyright (C) 2015 Jarle Hansen
    This library is free software; you can redistribute it and/or
    modify it under the terms of the GNU Lesser General Public
    License as published by the Free Software Foundation; either
    version 2.1 of the License, or (at your option) any later version.
    This library is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
    Lesser General Public License for more details.
    You should have received a copy of the GNU Lesser General Public
    License along with this library; if not, write to the Free Software
    Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301  USA
*/
#endregion
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Diagnostics;
using System.Collections.Specialized;
using System.Reflection;
using System.Reflection.Emit;

namespace Kraggs.TSM7.Data
{
    /// <summary>
    /// Generic delegate for creating objects.
    /// </summary>
    /// <returns></returns>
    internal delegate object CreateObject();
    /// <summary>
    /// Generic delegate for setting objects.
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    internal delegate object GenericGetter(object obj);
    /// <summary>
    /// Generic delegate for setting value on objects.
    /// </summary>
    /// <param name="target"></param>
    /// <param name="value">value to set.</param>
    /// <returns></returns>
    internal delegate object GenericSetter(object target, object value);

    /// <summary>
    /// Contains Reflection Utility functions.
    /// </summary>
    internal sealed partial class Reflection
    {
        static readonly Reflection sInstance = new Reflection();
        public static Reflection Instance { get { return sInstance; } }

        Reflection()
        { }

        public static CreateObject CreateConstructorMethod(Type objType, TypeInfo objTypeInfo = null)
        {
            CreateObject c;
            if (objTypeInfo == null)
                objTypeInfo = objType.GetTypeInfo();

            var n = objTypeInfo.Name + ".ctor";
            if(objTypeInfo.IsClass)
            {
                var dynMethod = new DynamicMethod(n, objType, Type.EmptyTypes);
                var ilGen = dynMethod.GetILGenerator();

                // Get the default constructor.
                var ct = objTypeInfo.DeclaredConstructors.Where((x) => x.GetParameters().Length == 0).FirstOrDefault();

                //var ct = objTypeInfo.GetConstructor(Type.EmptyTypes)
                //    ?? objTypeInfo.GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, null, Type.EmptyTypes, null);
                if (ct == null)
                    return null;
                ilGen.Emit(OpCodes.Newobj, ct);
                ilGen.Emit(OpCodes.Ret);
                c = (CreateObject)dynMethod.CreateDelegate(typeof(CreateObject));
            }
            else
            {
                var dynMethod = new DynamicMethod(n, typeof(object), null, objType);
                var ilGen = dynMethod.GetILGenerator();
                var lv = ilGen.DeclareLocal(objType);
                ilGen.Emit(OpCodes.Ldloca_S, lv);
                ilGen.Emit(OpCodes.Initobj, objType);
                ilGen.Emit(OpCodes.Ldloc_0);
                ilGen.Emit(OpCodes.Box, objType);
                ilGen.Emit(OpCodes.Ret);
                c = (CreateObject)dynMethod.CreateDelegate(typeof(CreateObject));
            }

            return c;
        }

        internal static GenericSetter CreateSetField(FieldInfo fieldInfo)
        {
            var type = fieldInfo.DeclaringType;
            var typeInfo = type.GetTypeInfo();

            var arguments = new Type[2];
            arguments[0] = arguments[1] = typeof(object);

            var dynSet = new DynamicMethod(fieldInfo.Name, typeof(object), arguments, type, true);
            var ilGen = dynSet.GetILGenerator();

            if (!typeInfo.IsClass)
            {
                var lv = ilGen.DeclareLocal(type);
                ilGen.Emit(OpCodes.Ldarg_0);
                ilGen.Emit(OpCodes.Unbox_Any, type);
                ilGen.Emit(OpCodes.Stloc_0);
                ilGen.Emit(OpCodes.Ldloca_S, lv);
                ilGen.Emit(OpCodes.Ldarg_1);
                if (fieldInfo.FieldType.GetTypeInfo().IsClass)
                    ilGen.Emit(OpCodes.Castclass, fieldInfo.FieldType);
                else
                    ilGen.Emit(OpCodes.Unbox_Any, fieldInfo.FieldType);
                ilGen.Emit(OpCodes.Stfld, fieldInfo);
                ilGen.Emit(OpCodes.Ldloc_0);
                ilGen.Emit(OpCodes.Box, type);
                ilGen.Emit(OpCodes.Ret);
            }
            else
            {
                ilGen.Emit(OpCodes.Ldarg_0);
                ilGen.Emit(OpCodes.Ldarg_1);
                if (fieldInfo.FieldType.GetTypeInfo().IsValueType)
                    ilGen.Emit(OpCodes.Unbox_Any, fieldInfo.FieldType);
                ilGen.Emit(OpCodes.Stfld, fieldInfo);
                ilGen.Emit(OpCodes.Ldarg_0);
                ilGen.Emit(OpCodes.Ret);
            }
            return (GenericSetter)dynSet.CreateDelegate(typeof(GenericSetter));
        }

        internal static GenericSetter CreateSetProperty(PropertyInfo propertyInfo)
        {
            // apperantly getting private methods dont work later on.
            // so for now disable getting private properties.
            //var setMethod = propertyInfo.GetSetMethod(true);
            //var setMethod = propertyInfo.GetSetMethod();
            var setMethod = propertyInfo.SetMethod;

            if (setMethod == null)
                return null;

            var type = propertyInfo.DeclaringType;
            var pt = propertyInfo.PropertyType;
            var ptInfo = pt.GetTypeInfo();
            var arguments = new Type[2];
            arguments[0] = arguments[1] = typeof(object);

            var dynSet = new DynamicMethod(setMethod.Name, typeof(object), arguments);
            var ilGen = dynSet.GetILGenerator();

            if (!type.GetTypeInfo().IsClass)
            {
                var lv = ilGen.DeclareLocal(type);
                ilGen.Emit(OpCodes.Ldarg_0);
                ilGen.Emit(OpCodes.Unbox_Any, type);
                ilGen.Emit(OpCodes.Stloc_0);
                ilGen.Emit(OpCodes.Ldloca_S, lv);
                ilGen.Emit(OpCodes.Ldarg_1);
                if (ptInfo.IsClass)
                    ilGen.Emit(OpCodes.Castclass, pt);
                else
                    ilGen.Emit(OpCodes.Unbox_Any, pt);
                ilGen.EmitCall(OpCodes.Call, setMethod, null);
                ilGen.Emit(OpCodes.Ldloc_0);
                ilGen.Emit(OpCodes.Ret);
            }
            else
            {
                if (setMethod.IsStatic)
                {
                    ilGen.Emit(OpCodes.Ldarg_1);
                    if (ptInfo.IsClass)
                        ilGen.Emit(OpCodes.Castclass, pt);
                    else
                        ilGen.Emit(OpCodes.Unbox_Any, pt);
                    ilGen.EmitCall(OpCodes.Call, setMethod, null);
                }
                else
                {
                    ilGen.Emit(OpCodes.Ldarg_0);
                    ilGen.Emit(OpCodes.Castclass, type);
                    ilGen.Emit(OpCodes.Ldarg_1);
                    if (ptInfo.IsClass)
                        ilGen.Emit(OpCodes.Castclass, pt);
                    else
                        ilGen.Emit(OpCodes.Unbox_Any, pt);
                    ilGen.EmitCall(OpCodes.Callvirt, setMethod, null);
                }
                ilGen.Emit(OpCodes.Ldarg_0);
            }
            ilGen.Emit(OpCodes.Ret);

            return (GenericSetter)dynSet.CreateDelegate(typeof(GenericSetter));
        }

    }
}
