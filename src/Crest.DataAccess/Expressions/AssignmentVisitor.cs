// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.DataAccess.Expressions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection;

    /// <summary>
    /// Looks for assignments between properties in an expression.
    /// </summary>
    internal sealed class AssignmentVisitor : ExpressionVisitor
    {
        private readonly Dictionary<Expression, Expression> assigmentExpressions =
            new Dictionary<Expression, Expression>();

        private readonly Type destinationType;

        private readonly Dictionary<MemberInfo, Expression> memberAssignments =
                    new Dictionary<MemberInfo, Expression>();

        private readonly List<MemberExpression> members = new List<MemberExpression>();
        private readonly Type sourceType;

        /// <summary>
        /// Initializes a new instance of the <see cref="AssignmentVisitor"/> class.
        /// </summary>
        /// <param name="sourceType">The type on the right of an assignment.</param>
        /// <param name="destinationType">The type on the left of an assignment.</param>
        public AssignmentVisitor(Type sourceType, Type destinationType)
        {
            this.destinationType = destinationType;
            this.sourceType = sourceType;
        }

        /// <summary>
        /// Gets the found assignments.
        /// </summary>
        /// <param name="expression">The expression to inspect.</param>
        /// <returns>
        /// The member from source linked to the expression for the destination.
        /// </returns>
        internal IEnumerable<KeyValuePair<MemberInfo, Expression>> GetAssignments(Expression expression)
        {
            this.assigmentExpressions.Clear();
            this.members.Clear();
            this.memberAssignments.Clear();
            this.Visit(expression);
            return this.memberAssignments;
        }

        /// <inheritdoc />
        protected override Expression VisitBinary(BinaryExpression node)
        {
            if (node.NodeType == ExpressionType.Assign)
            {
                MemberExpression destination = this.GetMember(node.Left);
                if ((destination != null) && IsRootObjectType(destination, this.destinationType))
                {
                    MemberExpression source = this.FindMemberFromAssignments(node.Right);
                    if ((source != null) && IsRootObjectType(source, this.sourceType))
                    {
                        this.memberAssignments[source.Member] = destination;
                    }
                }
                else
                {
                    this.assigmentExpressions[node.Left] = node.Right;
                }
            }

            return base.VisitBinary(node);
        }

        /// <inheritdoc />
        protected override Expression VisitMember(MemberExpression node)
        {
            this.members.Add(node);
            return base.VisitMember(node);
        }

        private static bool IsRootObjectType(MemberExpression expression, Type type)
        {
            while (expression.Expression.NodeType == ExpressionType.MemberAccess)
            {
                expression = (MemberExpression)expression.Expression;
            }

            return expression.Member.DeclaringType == type;
        }

        private MemberExpression FindExpressionFromConditional(Expression expression)
        {
            if (expression.NodeType == ExpressionType.Conditional)
            {
                var conditional = (ConditionalExpression)expression;
                return this.FindMemberFromAssignments(conditional.IfTrue) ??
                       this.FindMemberFromAssignments(conditional.IfFalse);
            }
            else
            {
                return null;
            }
        }

        private MemberExpression FindMemberFromAssignments(Expression expression)
        {
            MemberExpression member = this.GetMember(expression);
            if (member != null)
            {
                return member;
            }
            else if (this.assigmentExpressions.TryGetValue(expression, out Expression assignment))
            {
                return this.FindMemberFromAssignments(assignment);
            }
            else
            {
                return this.FindExpressionFromConditional(expression);
            }
        }

        private MemberExpression GetMember(Expression expression)
        {
            this.members.Clear();
            this.Visit(expression);
            return this.members.FirstOrDefault();
        }
    }
}
