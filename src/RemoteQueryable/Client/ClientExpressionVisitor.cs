﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions; 

namespace Sharp.RemoteQueryable.Client
{
  internal class ClientExpressionVisitor : ExpressionVisitor
  {
    public Expression Evaluate(Expression expression)
    {
      return base.Visit(expression);
    }

    private Expression EvaluateIfNeed(Expression expression)
    {
      var memberExpression = expression as MemberExpression;
      if (memberExpression != null)
      {
        if (memberExpression.Expression.Type.Name.Contains("DisplayClass"))
        {
          var rightValue = GetValue(memberExpression);
          return Expression.Constant(rightValue);
        }
      }

      var methodCallExpression = expression as MethodCallExpression;
      if (methodCallExpression != null)
      {
        var obj = ((ConstantExpression) methodCallExpression.Object).Value;
        var result = methodCallExpression.Method.Invoke(obj,
         methodCallExpression.Arguments.Select(a => GetValue(a as MemberExpression)).ToArray());

         return Expression.Constant(result);
      }

      return expression;
    }

    protected override Expression VisitBinary(BinaryExpression b)
    {
      Expression left = this.EvaluateIfNeed(this.Visit(b.Left));
      Expression right = this.EvaluateIfNeed(this.Visit(b.Right));
      Expression conversion = this.Visit(b.Conversion);
      if (left != b.Left || right != b.Right || conversion != b.Conversion)
      {
        if (b.NodeType == ExpressionType.Coalesce && b.Conversion != null)
          return Expression.Coalesce(left, right, conversion as LambdaExpression);
        else
          return Expression.MakeBinary(b.NodeType, left, right, b.IsLiftedToNull, b.Method);
      }
      return b;
    }

    private static object GetValue(MemberExpression exp)
    {
      var constantExpression = exp.Expression as ConstantExpression;
      if (constantExpression != null)
      {
        return (constantExpression.Value)
                .GetType()
                .GetField(exp.Member.Name)
                .GetValue(constantExpression.Value);
      }

      var expression = exp.Expression as MemberExpression;
      if (expression != null)
      {
        return GetValue(expression);
      }

      return null;
    }
  }
}