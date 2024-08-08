﻿namespace ServicesLayer.Models.NodeApi;

public class SecuredStatusResponse
{
	public string AuthKeyHmacSha512Base64 { get; set; }

	public SecuredStatusResponse(string authKeyHmacSha512Base64)
	{
		this.AuthKeyHmacSha512Base64 = authKeyHmacSha512Base64;
	}
}
