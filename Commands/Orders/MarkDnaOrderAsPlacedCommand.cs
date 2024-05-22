using System;
using WildHealth.Application.Commands._Base;
using WildHealth.Common.Models.Orders;
using WildHealth.Domain.Entities.Orders;
using FluentValidation;
using MediatR;

namespace WildHealth.Application.Commands.Orders
{
    public class MarkDnaOrderAsPlacedCommand : IRequest<DnaOrder>, IValidatabe
    {
        public int Id { get; }
        
        public string Number { get; }
        
        public DateTime Date { get; }

        public PlaceOrderItemModel[] Items { get; }

        public MarkDnaOrderAsPlacedCommand(
            int id,
            string number,
            DateTime date,
            PlaceOrderItemModel[] items)
        {
            Id = id;
            Number = number;
            Date = date;
            Items = items;
        }
        
        #region validation
        
        private class Validator: AbstractValidator<MarkDnaOrderAsPlacedCommand>
        {
            public Validator()
            {
                RuleFor(x => x.Id).GreaterThan(0);
                RuleFor(x => x.Number).NotNull().NotEmpty();
                RuleFor(x => x.Date).NotEmpty();
                RuleFor(x => x.Items)
                    .ForEach(x => x.SetValidator(new PlaceOrderItemModel.Validator()));
            }
        }

        /// <summary>
        /// <see cref="IValidatabe.IsValid"/>
        /// </summary>
        /// <returns></returns>
        public bool IsValid() => new Validator().Validate(this).IsValid;

        /// <summary>
        /// <see cref="IValidatabe.Validate"/>
        /// </summary>
        public void Validate() => new Validator().ValidateAndThrow(this);
        
        #endregion
    }
}