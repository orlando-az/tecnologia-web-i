# Guía de Instalación y Configuración de Servidor DNS
## Windows Server 2019 en VirtualBox

---

## Parte 0: Descargar lo necesario

**1. VirtualBox**
- Ir a: https://www.virtualbox.org/wiki/Downloads
- Descargar: **VirtualBox for Windows hosts**
- Instalar con todas las opciones por defecto

**2. ISO de Windows Server 2019**
- Ir a: https://www.microsoft.com/es-es/evalcenter/evaluate-windows-server-2019
- Registrarse con correo y descargar gratis (evaluación 180 días)
- El archivo pesa aproximadamente 5 GB

---

## Parte 1: Crear la máquina virtual

1. Abrir VirtualBox → clic en **New**
2. Completar:
   - Name: `ServidorDNS`
   - ISO: seleccionar la ISO descargada
   - Type: **Microsoft Windows**
   - Version: **Windows 2019 (64-bit)**
3. Marcar **Skip Unattended Installation** → Next

**Hardware:**
- RAM: `2048 MB` mínimo / `4096 MB` recomendado
- Processors: `2`
- Next

**Disco:**
- Create a Virtual Hard Disk Now
- Size: `60 GB`
- Next → **Finish**

---

## Parte 2: Configurar red Host-only en VirtualBox

Antes de configurar los adaptadores hay que crear la red Host-only:

1. En VirtualBox ir a **File → Tools → Network Manager**
2. Clic en pestaña **Host-only Networks**
3. Clic en **Create**
4. Verificar que tenga:
   - IPv4 Address: `192.168.56.1`
   - IPv4 Network Mask: `255.255.255.0`
5. **Apply → Close**

---

## Parte 3: Configurar los 2 adaptadores de red

1. Seleccionar la VM → **Settings → Network**

**Adapter 1 — Internet:**
- Marcar **Enable Network Adapter**
- Attached to: **NAT**
- Dejar todo por defecto

**Adapter 2 — DNS:**
- Clic en pestaña **Adapter 2**
- Marcar **Enable Network Adapter**
- Attached to: **Adaptador sólo anfitrión**
- Name: `VirtualBox Host-Only Ethernet Adapter`

2. OK

---

## Parte 4: Instalar Windows Server 2019

1. Seleccionar la VM → **Start**
2. En el instalador:
   - Language: **English** → Next
   - Clic en **Install Now**
3. Edición: **Windows Server 2019 Standard (Desktop Experience)**
4. Aceptar licencia → **Custom: Install Windows only (advanced)**
5. Seleccionar el disco → Next
6. Esperar 15 a 25 minutos
7. Al reiniciar crear contraseña de **Administrator**

> **Tip:** Si el mouse queda atrapado dentro de la VM presiona **Right Ctrl** para liberarlo

---

## Parte 5: Instalar Guest Additions

Permite pantalla completa y mejor rendimiento:

1. Con la VM iniciada ir a **Devices → Insert Guest Additions CD Image**
2. Dentro de la VM abrir **File Explorer**
3. Ir a la unidad de CD → ejecutar `VBoxWindowsAdditions.exe`
4. Instalar con todas las opciones por defecto
5. Reiniciar la VM

---

## Parte 6: Configurar IP estática

> **¿Para qué sirve?** Un servidor DNS siempre debe tener una IP fija porque los clientes de la red necesitan saber exactamente a qué dirección preguntarle. Si la IP cambia cada vez que se reinicia el servidor, los clientes dejan de encontrarlo y toda la resolución de nombres falla.

Dentro de la VM tendrás 2 interfaces de red:

**Ethernet 1 (NAT) — NO tocar**
- Dejar en DHCP automático
- VirtualBox asigna: `10.0.2.15`
- Esta interfaz da internet a la VM

**Ethernet 2 (Host-only) — Configurar aquí**

1. **Control Panel → Network and Sharing Center → Change adapter settings**
2. Identificar **Ethernet 2** → clic derecho → **Properties**
3. Seleccionar **Internet Protocol Version 4 (TCP/IPv4)** → **Properties**
4. Marcar **Use the following IP address:**

```
IP address:           192.168.56.10
Subnet mask:          255.255.255.0
Default gateway:      dejar vacío
Preferred DNS server: 127.0.0.1
Alternate DNS server: 8.8.8.8
```

> **¿Por qué Preferred DNS = 127.0.0.1?** Porque `127.0.0.1` significa "apuntarse a sí mismo". El servidor DNS debe consultarse a sí mismo primero antes de preguntar a otro servidor externo. Si se pone otra IP aquí el servidor no resolvería sus propias zonas correctamente.

5. OK → Close

**Verificar en CMD:**
```cmd
ipconfig
ping 8.8.8.8
```
Debes ver las 2 interfaces y el ping debe responder

---

## Parte 7: Instalar el rol DNS

> **¿Para qué sirve?** Windows Server no viene con el servicio DNS activo por defecto. Instalando este rol se activa el servicio que permite al servidor recibir consultas DNS, gestionar zonas y responder con registros. Sin este rol el servidor es simplemente una PC con Windows, no un servidor DNS.

1. Abrir **Server Manager**
2. Clic en **Add roles and features**
3. **Before You Begin** → **Next**
4. Installation Type → **Role-based or feature-based installation** → **Next**
5. Server Selection → **Select a server from the server pool** → seleccionar el servidor de la lista → **Next**
6. Server Roles → marcar **DNS Server**
7. Clic en **Add Features** cuando aparezca el cuadro
8. **Next → Next → Next → Install**
9. Esperar que finalice → **Close**

**Verificar:** Tools → **DNS** debe aparecer en el menú

---

## Parte 8: Crear zona directa

> **¿Para qué sirve?** La zona directa es la base del servidor DNS. Es la base de datos donde se guardan todos los registros que traducen nombres a IPs. Sin una zona directa el servidor no tiene ninguna información que responder cuando alguien pregunta por un nombre de dominio. Por ejemplo sin esta zona `nslookup www.miempresa.local` no encontraría nada.

1. **Tools → DNS Manager**
2. Expandir el servidor → clic derecho en **Forward Lookup Zones**
3. **New Zone** → Next
4. Zone type: **Primary zone** → Next

> **¿Por qué Primary zone?** Porque es la zona original y editable. Es donde realmente se crean y modifican los registros. Una zona secundaria solo es una copia de solo lectura que se usa para redundancia.

5. Zone name: `miempresa.local` → Next

> **¿Por qué .local?** Por convención los dominios internos de red local usan `.local` para diferenciarlos de dominios reales de internet. Nunca se debe usar un dominio real como `.com` para una red interna porque podría generar conflictos con internet.

6. Dejar archivo por defecto → Next
7. Dynamic update: **Do not allow dynamic updates** → Next

> **¿Por qué no permitir actualizaciones dinámicas?** Porque en una práctica controlada es mejor agregar los registros manualmente para aprender cómo funciona cada uno. Las actualizaciones dinámicas son útiles en entornos con Active Directory donde los equipos se registran solos.

8. **Finish**

---

## Parte 9: Agregar registros DNS

> **¿Para qué sirven los registros?** Los registros son las entradas individuales dentro de la zona. Cada registro le dice al servidor DNS cómo responder cuando alguien pregunta por un nombre específico. Sin registros la zona existe pero está vacía y no resuelve nada.

Dentro de la zona `miempresa.local`:

**Registro A (nombre → IP)**

> **¿Para qué sirve el registro A?** Es el registro más importante. Traduce un nombre de host a una dirección IPv4. Cuando un cliente pregunta "¿cuál es la IP de ns1.miempresa.local?" el servidor responde con la IP guardada en este registro.

1. Clic derecho en la zona → **New Host (A or AAAA)**
2. Name: `ns1`
3. IP address: `192.168.56.10`
4. **Add Host**

**Registro CNAME (alias)**

> **¿Para qué sirve el CNAME?** Crea un alias o nombre alternativo que apunta a otro nombre ya existente. En lugar de repetir la misma IP en múltiples registros A, el CNAME apunta a un registro A y hereda su IP automáticamente. Si la IP cambia solo se actualiza el registro A y todos los CNAME se actualizan solos.

1. Clic derecho → **New Alias (CNAME)**
2. Alias name: `www`
3. Fully qualified domain name: `ns1.miempresa.local`
4. OK

---

## Parte 10: Crear zona inversa

> **¿Para qué sirve la zona inversa?** Hace lo contrario de la zona directa: traduce una IP a un nombre. Esto es útil para verificar la identidad de un servidor, para logs de seguridad, para servidores de correo que verifican si una IP tiene nombre asignado y para herramientas de diagnóstico de red. Sin zona inversa `nslookup -type=PTR` no devolvería ningún resultado.

1. Clic derecho en **Reverse Lookup Zones** → **New Zone**
2. Primary zone → Next
3. **IPv4 Reverse Lookup Zone** → Next
4. Network ID: `192.168.56` → Next

> **¿Qué es el Network ID?** Son los primeros 3 octetos de tu red. Windows los toma y crea automáticamente la zona con el nombre `56.168.192.in-addr.arpa` que es el formato estándar para zonas inversas.

5. Windows crea: `56.168.192.in-addr.arpa`
6. Next → Next → **Finish**

**Agregar registro PTR:**

> **¿Para qué sirve el registro PTR?** Es el registro de la zona inversa. Cuando alguien pregunta "¿a qué nombre pertenece la IP 192.168.56.10?" el servidor responde con el nombre guardado en este registro PTR.

1. Clic derecho en zona inversa → **New Pointer (PTR)**
2. Host IP address: `10`
3. Host name: `ns1.miempresa.local`
4. OK

---

## Parte 11: Configurar reenviadores

> **¿Para qué sirven los reenviadores?** Cuando alguien pregunta al servidor DNS por un nombre que no está en sus zonas locales, como `google.com`, el servidor por sí solo no sabría responder. Los reenviadores le dicen al servidor DNS a quién preguntarle en ese caso. Sin reenviadores el servidor solo resolvería nombres locales y fallaría al intentar resolver cualquier nombre de internet.

1. Abrir **Tools → DNS Manager**
2. En el panel izquierdo clic derecho sobre el nombre del servidor → **Properties**
3. Clic en la pestaña **Forwarders**
4. Clic en **Edit**
5. Escribir `8.8.8.8` → Enter
6. Escribir `1.1.1.1` → Enter
7. OK → **Apply** → OK

```
DNS Manager
└── WIN-XXXXXXX  ← clic derecho aquí
    ├── Forward Lookup Zones
    │   └── miempresa.local
    └── Reverse Lookup Zones
        └── 56.168.192.in-addr.arpa
```

---

## Parte 12: Verificar con nslookup

> **¿Para qué sirve nslookup?** Es la herramienta principal para probar y diagnosticar un servidor DNS. Permite consultar cualquier tipo de registro DNS y ver exactamente qué responde el servidor. Es la forma más directa de confirmar que todo lo configurado está funcionando correctamente.

```cmd
# Verificar registro A
nslookup ns1.miempresa.local 192.168.56.10

# Verificar alias www
nslookup www.miempresa.local 192.168.56.10

# Verificar zona inversa
nslookup -type=PTR 192.168.56.10 192.168.56.10

# Verificar internet (reenviadores)
nslookup google.com 192.168.56.10
```

**Resultado esperado registro A:**
```
Server:  ns1.miempresa.local
Address: 192.168.56.10

Name:    ns1.miempresa.local
Address: 192.168.56.10
```

**Resultado esperado CNAME:**
```
Server:  ns1.miempresa.local
Address: 192.168.56.10

Name:    ns1.miempresa.local
Address: 192.168.56.10
Aliases: www.miempresa.local
```

---

## Errores comunes

| Error | Causa | Solución |
|-------|-------|----------|
| VM no arranca | Virtualización desactivada | Activar VT-x en BIOS |
| Pantalla negra | Video sin configurar | Settings → Display → Video Memory 128MB |
| Mouse atrapado | Normal en VirtualBox | Presionar **Right Ctrl** |
| VM muy lenta | Poca RAM | Asignar mínimo 2GB |
| Adapter 2 desactivado | Red Host-only no creada | Crear en File → Tools → Network Manager |
| Solo veo 1 interfaz en VM | Adapter 2 no habilitado | Verificar Settings → Network → Adapter 2 |
| nslookup consulta al ISP | Preferred DNS mal configurado | Usar `nslookup nombre 192.168.56.10` |
| No resuelve internet | Sin reenviadores | Agregar 8.8.8.8 en Forwarders |
| nslookup no responde | IP mal configurada | Verificar Preferred DNS = 127.0.0.1 en Ethernet 2 |
| Non-existent domain | Zona incorrecta | Verificar nombre exacto de zona |
| ping 8.8.8.8 falla | Ethernet 1 con IP fija | Verificar que Ethernet 1 sigue en DHCP |
