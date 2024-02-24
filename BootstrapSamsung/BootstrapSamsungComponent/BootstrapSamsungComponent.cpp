// BootstrapSamsungComponent.cpp
#include "pch.h"
#include "BootstrapSamsungComponent.h"

using namespace BootstrapSamsungComponent;
using namespace RPCComponent;

uint32 NativeComponent::UnlockCapability ()
{
	CRPCComponent::Initialize();
	unsigned outval = 0x0;
	CRPCComponent::Registry_SetString(
		(unsigned)HKEY_LOCAL_MACHINE,
		ref new String(L"SOFTWARE\\Microsoft\\SecurityManager\\CapabilityClasses"),
//		ref new String(L"ID_CAP_DEVELOPERUNLOCK_API"),
		ref new String(L"ID_CAP_PRIV_SECMIGRATOR"),
		ref new String(L"CAPABILITY_CLASS_THIRD_PARTY_APPLICATIONS\0\0", 43),
		&outval);
	return outval;
}