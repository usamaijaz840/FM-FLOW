using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FMFlow.Entities;

namespace FMFlow.Entities;

public class NonceConfiguration()
{
	public string Expiration { get; set; } = "00:00:00";

	public int NonceLengthInBytes { get; set; } = 0;

}
