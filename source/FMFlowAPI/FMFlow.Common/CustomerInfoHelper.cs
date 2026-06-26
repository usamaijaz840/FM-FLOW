using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FMFlow.Entities;

namespace FMFlow.Common;

public class CustomerInfoHelper
{

	/// <summary>
	/// Gets the customer's email address, falling back to Lead.Email if Customer is null
	/// </summary>
	public static string GetCustomerEmail(Lead lead)
	{
		return lead.Customer?.Email ?? lead.Email;
	}

	/// <summary>
	/// Gets the customer's full name, falling back to Lead.GetFullName() if Customer is null
	/// </summary>
	public static string GetCustomerFullName(Lead lead)
	{
		return lead.Customer?.GetFullName() ?? lead.GetFullName();
	}

	public static string GetCustomerPhoneNumber(Lead lead)
	{
		return lead.Customer?.PhoneNumber ?? lead.Mobile;
	}
}
