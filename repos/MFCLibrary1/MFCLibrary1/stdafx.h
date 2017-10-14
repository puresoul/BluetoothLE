// stdafx.h: soubor k zahrnutí pro standardní systémové soubory k zahrnutí,
// nebo často používané soubory k zahrnutí specifické pro projekt,
// které se mění jen zřídka

#pragma once

#ifndef VC_EXTRALEAN
#define VC_EXTRALEAN            // Vyloučit málo používané položky z hlavičkových souborů Windows
#endif

#include "targetver.h"

#define _ATL_CSTRING_EXPLICIT_CONSTRUCTORS      // některé konstruktory CString budou explicitní

#include <afxwin.h>         // základní a standardní součásti MFC
#include <afxext.h>         // rozšíření MFC

#ifndef _AFX_NO_OLE_SUPPORT
#include <afxole.h>         // Třídy MFC OLE
#include <afxodlgs.h>       // Třídy dialogu MFC OLE
#include <afxdisp.h>        // Automatizační třídy MFC
#endif // _AFX_NO_OLE_SUPPORT

#ifndef _AFX_NO_DB_SUPPORT
#include <afxdb.h>                      // třídy databází MFC ODBC
#endif // _AFX_NO_DB_SUPPORT

#ifndef _AFX_NO_DAO_SUPPORT
#include <afxdao.h>                     // třídy databází MFC DAO
#endif // _AFX_NO_DAO_SUPPORT

#ifndef _AFX_NO_OLE_SUPPORT
#include <afxdtctl.h>           // podpora MFC pro běžné ovládací prvky Internet Exploreru 4
#endif
#ifndef _AFX_NO_AFXCMN_SUPPORT
#include <afxcmn.h>                     // podpora MFC pro běžné ovládací prvky Windows
#endif // _AFX_NO_AFXCMN_SUPPORT


