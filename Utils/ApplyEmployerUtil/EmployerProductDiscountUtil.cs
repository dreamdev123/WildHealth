using System.Net;
using System.Collections.Generic;
using WildHealth.Application.Extensions;
using WildHealth.Domain.Entities.AddOns;
using WildHealth.Domain.Entities.EmployerProducts;
using WildHealth.Domain.Entities.Payments;
using WildHealth.Domain.Entities.Products;
using WildHealth.Domain.Enums.Products;
using WildHealth.Shared.Exceptions;

namespace WildHealth.Application.Utils.ApplyEmployerUtil;

public class EmployerProductDiscountUtil: IEmployerProductDiscountUtil
{
    /// <summary>
    /// <see cref="IEmployerProductDiscountUtil.ApplyDiscount(PaymentPrice, EmployerProduct)"/>
    /// </summary>
    /// <param name="paymentPrice"></param>
    /// <param name="employerProduct"></param>
    public void ApplyDiscount(PaymentPrice paymentPrice, EmployerProduct employerProduct)
    {
        if (employerProduct is null)
        {
            return;
        }
        
        paymentPrice.OverwritePrice(employerProduct.GetPatientPrice(ProductType.Membership, paymentPrice.Price));
        paymentPrice.OverwriteStartupFee(employerProduct.GetPatientPrice(ProductType.StartupFee, paymentPrice.StartupFee));
        paymentPrice.OverwriteDiscount(paymentPrice.Discount + employerProduct.GetPatientDiscount(ProductType.Membership, paymentPrice.Price));
    }

    /// <summary>
    /// <see cref="IEmployerProductDiscountUtil.ApplyDiscount(PaymentPlan, EmployerProduct)"/>
    /// </summary>
    /// <param name="paymentPlan"></param>
    /// <param name="employerProduct"></param>
    public void ApplyDiscount(PaymentPlan paymentPlan, EmployerProduct employerProduct)
    {
        foreach (var period in paymentPlan.PaymentPeriods)
        {
            foreach (var price in period.Prices)
            {
                ApplyDiscount(price, employerProduct);
            }
        }
    }

    /// <summary>
    /// <see cref="IEmployerProductDiscountUtil.ApplyDiscount(ICollection{AddOn}, EmployerProduct)"/>
    /// </summary>
    /// <param name="addOns"></param>
    /// <param name="employerProduct"></param>
    public void ApplyDiscount(ICollection<AddOn> addOns, EmployerProduct employerProduct)
    {
        if (employerProduct is null)
        {
            return;
        }
        
        foreach (var addOn in addOns)
        {
            ApplyDiscount(addOn, employerProduct, null);
        }
    }

    /// <summary>
    /// Apply employer product discount to add-on.  If the referenceAddOn is provided, then determine discount based on that addOn
    /// </summary>
    /// <param name="addOn"></param>
    /// <param name="employerProduct"></param>
    /// <param name="referenceAddOn"></param>
    public void ApplyDiscount(AddOn addOn, EmployerProduct? employerProduct, AddOn? referenceAddOn)
    {
        if (employerProduct is null)
        {
            return;
        }

        var discountAddOn = referenceAddOn ?? addOn;
        
        addOn.OverwritePrice(employerProduct.GetPatientPrice(ProductType.AddOns, addOn.Price, discountAddOn.GetId()));

        // AssertChildrenExist(addOn);
        // Updated this so we can now provide a "child" addOn and still get a result
        if (!addOn.Children.IsNullOrEmpty())
        {
            foreach (var addOnChild in addOn.Children)
            {
                addOnChild.Child.OverwritePrice(employerProduct.GetPatientPrice(ProductType.AddOns, addOnChild.Child.Price, discountAddOn.GetId()));
            }
        }
    }

    /// <summary>
    /// <see cref="IEmployerProductDiscountUtil.ApplyDiscount(Product, EmployerProduct)"/>
    /// </summary>
    /// <param name="product"></param>
    /// <param name="employerProduct"></param>
    public void ApplyDiscount(Product product, EmployerProduct employerProduct)
    {
        if (employerProduct is null)
        {
            return;
        }
        
        product.OverwritePrice(employerProduct.GetPatientPrice(product.Type, product.Price));
    }


    /// <summary>
    /// Applies full discount from the default employer
    /// </summary>
    /// <param name="product"></param>
    /// <param name="employerProduct"></param>
    public void ApplyDefaultEmployerDiscount(Product product, EmployerProduct? employerProduct)
    {
        if (employerProduct is null)
        {
            return;
        }
        
        if (employerProduct.IsDefault)
        {
            product.OverwritePrice(0);
        }
    }
    
    #region private
    
    /// <summary>
    /// Asserts add-on children exists
    /// </summary>
    /// <param name="addOn"></param>
    /// <exception cref="AppException"></exception>
    private void AssertChildrenExist(AddOn addOn)
    {
        //Empty is fine, but we don't want null.
        if (addOn.Children == null)
        {
            throw new AppException(HttpStatusCode.InternalServerError,$"The addon with id {addOn.Id} has no children.");
        }
    }
    
    #endregion
}