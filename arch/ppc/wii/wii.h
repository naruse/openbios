/*
 *   Creation Date: <2004/08/28 17:50:12 stepan>
 *   Time-stamp: <2004/08/28 17:50:12 stepan>
 *
 *	<wii.h>
 *
 *   Copyright (C) 2005 Stefan Reinauer
 *
 *   This program is free software; you can redistribute it and/or
 *   modify it under the terms of the GNU General Public License
 *   version 2
 *
 */

#ifndef _H_WII
#define _H_WII

#define ARRSIZE(x)    (sizeof(x) / (sizeof(x[0])))

/* vfd.c */
extern int		vfd_draw_str( const char *str );
extern void		vfd_close( void );

//
// Wii platform types.
//
extern int is_wii_rvl(void);
extern int is_wii_cafe(void);

#define ESPRESSO_PVR_HIGH       0x70010000

//
// Speeds.
//
#define WII_RVL_CPU_FREQ        729000000
#define WII_RVL_BUS_FREQ        243000000
#define WII_CAFE_CPU_FREQ       1243125000
#define WII_CAFE_BUS_FREQ       248625000

// Set to 0 to use direct XFB.
#define RVL_FB_BASE                             0x10000000
#define RVL_XFB_BASE                            0x1012C000

// USB Gecko serial console (console.c).
int rvl_gecko_probe(void);
int rvl_gecko_present(void);
int rvl_gecko_create_node(void);

// Optional RAM disk image placed in free MEM2 by the loader (see drivers/wii_ramdisk.c).
#define RVL_RAMDISK_BASE                        0x10400000

//
// Mac OS 9 address window. Mac OS reserves low logical addresses for RAM, so
// it cannot see the Wii's registers at their physical addresses. The device
// tree handed to Mac OS carries window addresses instead, and the patched
// Trampoline (tools/patches/trampoline-cave.S in the project) maps them:
//
//   0xDC000000-0xDDFFFFFF -> physical 0x0C000000-0x0DFFFFFF  (registers)
//   0xE0000000-0xE11FFFFF -> physical 0x10000000-0x111FFFFF  (MEM2 below the RAM bank)
//
// 0xDE000000-0xDEFFFFFF is where Mac OS finds devices the Wii does not have
// (VIA, Heathrow, ATA). Mac OS leaves it unmapped and emulates them on each
// fault. Under Open Firmware nothing emulates them, so the whole region is
// backed by one scratch page: the Trampoline's few writes there go nowhere.
//
// Open Firmware resolves the same addresses (see ea_to_phys in ofmem.c), so
// the Trampoline can reach the hardware through them before Mac OS starts.
//
#define RVL_OS9_WINDOW_OFFSET                   0xD0000000
#define RVL_OS9_IO_WINDOW_BASE                  0xDC000000
#define RVL_OS9_IO_WINDOW_END                   0xDE000000
#define RVL_OS9_EMU_BASE                        0xDE000000
#define RVL_OS9_EMU_END                         0xDF000000
#define RVL_OS9_MEM2_WINDOW_BASE                0xE0000000
#define RVL_OS9_MEM2_WINDOW_END                 0xE1200000

// Part of MEM2 handed to the OS as general RAM (MEM1_MAP_BASE in ofmem.c).
#define RVL_MEM2_BANK_BASE                      0x11200000
#define RVL_MEM2_BANK_SIZE                      0x02800000

#define CAFE_LATTE_IPCPPCMSG        0x0D800000
#define CAFE_LATTE_IPCPPCCTRL       0x0D800004
#define CAFE_CMD_POWEROFF           0xCAFE0001
#define CAFE_CMD_REBOOT             0xCAFE0002
#define CAFE_CMD_PRINT              0xCAFE6400

#include "kernel.h"

#endif   /* _H_WII */
