using System.Collections.Generic;
using WildHealth.Domain.Entities.AddOns;
using WildHealth.Domain.Entities.EmployerProducts;
using WildHealth.Domain.Entities.Payments;
using WildHealth.Domain.Entities.Products;

namespace WildHealth.Application.Utils.ApplyEmployerUtil;

public interface IEmployerProductDiscountUtil
{
    /// <summary>
    /// Apply employer product discount to payment price
    /// </summary>
    /// <param name="paymentPrice"></param>
    /// <param name="employerProduct"></param>
    public void ApplyDiscount(PaymentPrice paymentPrice, EmployerProduct employerProduct);
    
    /// <summary>
    /// Apply employer product discount to payment plan
    /// </summary>
    /// <param name="paymentPlan"></param>
    /// <param name="employerProduct"></param>
    public void ApplyDiscount(PaymentPlan paymentPlan, EmployerProduct employerProduct);
    
    /// <summary>
    /// Apply employer product to addons and children
    /// </summary>
    /// <param name="addOns"></param>
    /// <param name="employerProduct"></param>
    public void ApplyDiscount(ICollection<AddOn> addOns, EmployerProduct employerProduct);

    /// <summary>
    /// Apply employer product discount to add-on.  If the referenceAddOn is provided, then determine discount based on that addOn
    /// </summary>
    /// <param name="addOn"></param>
    /// <param name="employerProduct"></param>
    /// <param name="referenceAddOn"></param>
    public void ApplyDiscount(AddOn addOn, EmployerProduct? employerProduct, AddOn? referenceAddOn);
    
    /// <summary>
    /// Apply employer product discount to any product
    /// </summary>
    /// <param name="product"></param>
    /// <param name="employerProduct"></param>
    public void ApplyDiscount(Product product, EmployerProduct employerProduct);

    /// <summary>
    /// Applies full discount from the default employer
    /// </summary>
    /// <param name="product"></param>
    /// <param name="employerProduct"></param>
    public void ApplyDefaultEmployerDiscount(Product product, EmployerProduct? employerProduct);
}