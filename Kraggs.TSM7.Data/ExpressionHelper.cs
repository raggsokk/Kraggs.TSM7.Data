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
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Kraggs.TSM7.Data
{
    internal static class ExpressionHelper
    {
        internal static bool BuildWhereString(StringBuilder sb, Expression exp)
        {
            switch (exp.NodeType)
            {
                case ExpressionType.Lambda:
                    {
                        var l = exp as LambdaExpression;
                        return BuildWhereString(sb, l.Body);
                    }
                case ExpressionType.Constant:
                    {
                        var c = exp as ConstantExpression;
                        SafeValue(sb, c.Value, c.Type);
                        //if (c.Type == typeof(String))
                        //    sb.AppendFormat("'{0}'", c.Value);
                        //else if (c.Type == typeof(DateTime))
                        //    throw new NotImplementedException("SQL Where Safe DateTime");
                        //else if (c.Type == typeof(Guid))
                        //    throw new NotImplementedException("SQL Where Safe GUID");
                        //else
                        //    sb.Append(c.Value);
                        return true;
                    }
                case ExpressionType.MemberAccess:
                    {
                        var m = exp as MemberExpression;

                        clsTypeInfo myType = null;
                        if (Reflection.Instance.sTypeCache.TryGetValue(m.Member.DeclaringType.FullName, out myType))
                        {
                            var c = myType.MemberHash[m.Member.Name];
                            sb.Append(c.ColumnName);
                        }
                        else
                        {
                            var objectMember = Expression.Convert(m, typeof(object));
                            var getterLambda = Expression.Lambda<Func<object>>(objectMember);
                            var getter = getterLambda.Compile();
                            SafeValue(sb, getter(), m.Type);
                        }
                        return true;
                    }
                case ExpressionType.Equal:
                case ExpressionType.NotEqual:
                case ExpressionType.GreaterThan:
                case ExpressionType.GreaterThanOrEqual:
                case ExpressionType.LessThan:
                case ExpressionType.LessThanOrEqual:
                case ExpressionType.And:
                case ExpressionType.AndAlso:
                case ExpressionType.Or:
                case ExpressionType.OrElse:
                    {
                        var e = exp as BinaryExpression;
                        var left = BuildWhereString(sb, e.Left);
                        switch (exp.NodeType)
                        {
                            case ExpressionType.Equal:
                                sb.Append(" == "); break;
                            case ExpressionType.NotEqual:
                                sb.Append(" != "); break;
                            case ExpressionType.GreaterThan:
                                sb.Append(" > "); break;
                            case ExpressionType.GreaterThanOrEqual:
                                sb.Append(" >= "); break;
                            case ExpressionType.LessThan:
                                sb.Append(" < "); break;
                            case ExpressionType.LessThanOrEqual:
                                sb.Append(" <= "); break;
                            case ExpressionType.Add:
                                sb.Append(" & "); break;
                            case ExpressionType.AndAlso:
                                sb.Append(" && "); break;
                            case ExpressionType.Or:
                                sb.Append(" | "); break;
                            case ExpressionType.OrElse:
                                sb.Append(" || "); break;
                            default:
                                throw new NotImplementedException(exp.NodeType.ToString());
                        }

                        var right = BuildWhereString(sb, e.Right);
                        return left & right;
                    }
                default:
                    throw new NotImplementedException(exp.NodeType.ToString());
            }

            //return false;
        }

        internal static void SafeValue(StringBuilder sb, object obj, Type t)
        {
            if (t == typeof(string))
                sb.AppendFormat("'{0}'", obj);
            else if (t == typeof(DateTime))
                throw new NotImplementedException("SQL Where Safe DateTime");
            else if (t == typeof(Guid))
                throw new NotImplementedException("SQL Where Safe GUID");
            else
                sb.Append(obj);

        }

    }
}
