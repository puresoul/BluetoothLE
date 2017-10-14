// MFCLibrary1.h: soubor hlavních hlaviček pro knihovnu DLL MFCLibrary1
//

#pragma once

#ifndef __AFXWIN_H__
	#error "zahrnout soubor stdafx.h před zahrnutím tohoto souboru pro soubor PCH"
#endif

#include "resource.h"		// hlavní symboly


// CMFCLibrary1App
// Implementaci této třídy naleznete v souboru MFCLibrary1.cpp.
//

class CMFCLibrary1App : public CWinApp
{
public:
	CMFCLibrary1App();

// Přepsání
public:
	virtual BOOL InitInstance();

	DECLARE_MESSAGE_MAP()
};
