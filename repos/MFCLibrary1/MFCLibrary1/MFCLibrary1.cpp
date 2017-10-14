// MFCLibrary1.cpp: Definuje inicializační rutiny pro knihovnu DLL.
//

#include "stdafx.h"
#include "MFCLibrary1.h"

#ifdef _DEBUG
#define new DEBUG_NEW
#endif

//
//TODO: Pokud je tato knihovna DLL dynamicky propojená s knihovnami MFC DLL,
//		je potřeba na začátek všech funkcí exportovaných z této
//		knihovny DLL, které volají knihovnu MFC, přidat
//		makro AFX_MANAGE_STATE.
//
//		Příklad:
//
//		extern "C" BOOL PASCAL EXPORT ExportedFunction()
//		{
//			AFX_MANAGE_STATE(AfxGetStaticModuleState());
//			// sem tělo normální funkce
//		}
//
//		Je velmi důležité, aby v každé funkci bylo toto makro uvedené
//		před jakýmkoli voláním knihovny MFC. To znamená, že
//		se toto makro musí ve funkci nacházet jako první příkaz,
//		dokonce i před všemi deklaracemi objektových proměnných,
//		protože jejich konstruktory můžou generovat volání funkcí MFC
//		DLL.
//
//		Vyhledejte technické poznámky ke knihovnám MFC 33 a 58, ve kterých najdete další
//		podrobnosti.
//

// CMFCLibrary1App

BEGIN_MESSAGE_MAP(CMFCLibrary1App, CWinApp)
END_MESSAGE_MAP()


// CMFCLibrary1App – vytváření

CMFCLibrary1App::CMFCLibrary1App()
{
	// TODO: sem přidejte kód konstrukce
	// Umístěte veškerou důležitou inicializaci do funkce InitInstance.
}


// Jediný objekt CMFCLibrary1App

CMFCLibrary1App theApp;


// CMFCLibrary1App – inicializace

BOOL CMFCLibrary1App::InitInstance()
{
	CWinApp::InitInstance();

	return TRUE;
}

/*
** Zabbix
** Copyright (C) 2001-2017 Zabbix SIA
**
** This program is free software; you can redistribute it and/or modify
** it under the terms of the GNU General Public License as published by
** the Free Software Foundation; either version 2 of the License, or
** (at your option) any later version.
**
** This program is distributed in the hope that it will be useful,
** but WITHOUT ANY WARRANTY; without even the implied warranty of
** MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
** GNU General Public License for more details.
**
** You should have received a copy of the GNU General Public License
** along with this program; if not, write to the Free Software
** Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.
**/

#define ZBX_JSON_MAX_STRERROR	255

static char	zbx_json_strerror_message[ZBX_JSON_MAX_STRERROR];

const char	*zbx_json_strerror(void)
{
	return zbx_json_strerror_message;
}

void	zbx_json_init(struct zbx_json *j, size_t allocate)
{
	assert(j);

	j->buffer = NULL;
	j->buffer_allocated = 0;
	j->buffer_offset = 0;
	j->buffer_size = 0;
	j->status = ZBX_JSON_EMPTY;
	j->level = 0;
	__zbx_json_realloc(j, allocate);
	*j->buffer = '\0';

	zbx_json_addobject(j, NULL);
}

void	zbx_json_addobject(struct zbx_json *j, const char *name)
{
	__zbx_json_addobject(j, name, 1);
}

void	zbx_json_addarray(struct zbx_json *j, const char *name)
{
	__zbx_json_addobject(j, name, 0);
}

static void	__zbx_json_addobject(struct zbx_json *j, const char *name, int object)
{
	size_t	len = 2; /* brackets */
	char	*p, *psrc, *pdst;

	assert(j);

	if (ZBX_JSON_COMMA == j->status)
		len++; /* , */

	if (NULL != name)
	{
		len += __zbx_json_stringsize(name, ZBX_JSON_TYPE_STRING);
		len += 1; /* : */
	}

	__zbx_json_realloc(j, j->buffer_size + len + 1/*'\0'*/);

	psrc = j->buffer + j->buffer_offset;
	pdst = j->buffer + j->buffer_offset + len;

	memmove(pdst, psrc, j->buffer_size - j->buffer_offset + 1/*'\0'*/);

	p = psrc;

	if (ZBX_JSON_COMMA == j->status)
		*p++ = ',';

	if (NULL != name)
	{
		p = __zbx_json_insstring(p, name, ZBX_JSON_TYPE_STRING);
		*p++ = ':';
	}

	*p++ = object ? '{' : '[';
	*p = object ? '}' : ']';

	j->buffer_offset = p - j->buffer;
	j->buffer_size += len;
	j->level++;
	j->status = ZBX_JSON_EMPTY;
}


void	zbx_json_addstring(struct zbx_json *j, const char *name, const char *string, zbx_json_type_t type)
{
	size_t	len = 0;
	char	*p, *psrc, *pdst;

	assert(j);

	if (ZBX_JSON_COMMA == j->status)
		len++; /* , */

	if (NULL != name)
	{
		len += __zbx_json_stringsize(name, ZBX_JSON_TYPE_STRING);
		len += 1; /* : */
	}
	len += __zbx_json_stringsize(string, type);

	__zbx_json_realloc(j, j->buffer_size + len + 1/*'\0'*/);

	psrc = j->buffer + j->buffer_offset;
	pdst = j->buffer + j->buffer_offset + len;

	memmove(pdst, psrc, j->buffer_size - j->buffer_offset + 1/*'\0'*/);

	p = psrc;

	if (ZBX_JSON_COMMA == j->status)
		*p++ = ',';

	if (NULL != name)
	{
		p = __zbx_json_insstring(p, name, ZBX_JSON_TYPE_STRING);
		*p++ = ':';
	}
	p = __zbx_json_insstring(p, string, type);

	j->buffer_offset = p - j->buffer;
	j->buffer_size += len;
	j->status = ZBX_JSON_COMMA;
}

int	zbx_json_close(struct zbx_json *j)
{
	if (1 == j->level)
	{
		zbx_set_json_strerror("cannot close top level object");
		return FAIL;
	}

	j->level--;
	j->buffer_offset++;
	j->status = ZBX_JSON_COMMA;

	return SUCCEED;
}

void	zbx_json_free(struct zbx_json *j)
{
	assert(j);

	if (j->buffer != j->buf_stat)
		zbx_free(j->buffer);
}


const char	*progname = NULL;
const char	title_message[] = "";
const char	*usage_message[] = { NULL };

const char	*help_message[] = { NULL };

unsigned char	program_type = ZBX_PROGRAM_TYPE_SENDER;

int	zabbix_sender_send_values(const char *address, unsigned short port, const char *source,
	const zabbix_sender_value_t *values, int count, char **result)
{
	zbx_socket_t	sock;
	int		ret, i;
	struct zbx_json	json;

	if (1 > count)
	{
		if (NULL != result)
			*result = zbx_strdup(NULL, "values array must have at least one item");

		return FAIL;
	}

	zbx_json_init(&json, ZBX_JSON_STAT_BUF_LEN);
	zbx_json_addstring(&json, ZBX_PROTO_TAG_REQUEST, ZBX_PROTO_VALUE_SENDER_DATA, ZBX_JSON_TYPE_STRING);
	zbx_json_addarray(&json, ZBX_PROTO_TAG_DATA);

	for (i = 0; i < count; i++)
	{
		zbx_json_addobject(&json, NULL);
		zbx_json_addstring(&json, ZBX_PROTO_TAG_HOST, values[i].host, ZBX_JSON_TYPE_STRING);
		zbx_json_addstring(&json, ZBX_PROTO_TAG_KEY, values[i].key, ZBX_JSON_TYPE_STRING);
		zbx_json_addstring(&json, ZBX_PROTO_TAG_VALUE, values[i].value, ZBX_JSON_TYPE_STRING);
		zbx_json_close(&json);
	}
	zbx_json_close(&json);

	if (SUCCEED == (ret = zbx_tcp_connect(&sock, source, address, port, GET_SENDER_TIMEOUT,
		ZBX_TCP_SEC_UNENCRYPTED, NULL, NULL)))
	{
		if (SUCCEED == (ret = zbx_tcp_send(&sock, json.buffer)))
		{
			if (SUCCEED == (ret = zbx_tcp_recv(&sock)))
			{
				if (NULL != result)
					*result = zbx_strdup(NULL, sock.buffer);
			}
		}

		zbx_tcp_close(&sock);
	}

	if (FAIL == ret && NULL != result)
		*result = zbx_strdup(NULL, zbx_socket_strerror());

	zbx_json_free(&json);

	return ret;
}

int	zabbix_sender_parse_result(const char *result, int *response, zabbix_sender_info_t *info)
{
	int			ret;
	struct zbx_json_parse	jp;
	char			value[MAX_STRING_LEN];

	if (SUCCEED != (ret = zbx_json_open(result, &jp)))
		goto out;

	if (SUCCEED != (ret = zbx_json_value_by_name(&jp, ZBX_PROTO_TAG_RESPONSE, value, sizeof(value))))
		goto out;

	*response = (0 == strcmp(value, ZBX_PROTO_VALUE_SUCCESS)) ? 0 : -1;

	if (NULL == info)
		goto out;

	if (SUCCEED != zbx_json_value_by_name(&jp, ZBX_PROTO_TAG_INFO, value, sizeof(value)) ||
		3 != sscanf(value, "processed: %*d; failed: %d; total: %d; seconds spent: %lf",
			&info->failed, &info->total, &info->time_spent))
	{
		info->total = -1;
	}
out:
	return ret;
}

void	zabbix_sender_free_result(void *ptr)
{
	if (NULL != ptr)
		free(ptr);
}
