\   Wii specific initialization code
\
\   Copyright (C) 2005 Stefan Reinauer
\   Copyright (C) 2025 John Davis
\
\   This program is free software; you can redistribute it and/or
\   modify it under the terms of the GNU General Public License
\   as published by the Free Software Foundation
\


\ -------------------------------------------------------------------------
\ initialization
\ -------------------------------------------------------------------------

: make-openable ( path )
  find-dev if
    begin ?dup while
      \ install trivial open and close methods
      dup active-package! is-open
      parent
    repeat
  then
;

: preopen ( chosen-str node-path )
  2dup make-openable

  " /chosen" find-device
  open-dev ?dup if
    encode-int 2swap property
  else
    2drop
  then
;

\ preopen device nodes (and store the ihandles under /chosen)
:noname
  " rtc" " rtc" preopen
  " memory" " /memory" preopen
; SYSTEM-initializer


\ use the tty interface if available
: activate-tty-interface
  " /packages/terminal-emulator" find-dev if drop
  then
;

variable keyboard-phandle 0 keyboard-phandle !

: (find-keyboard-device) ( phandle -- )
  recursive
  keyboard-phandle @ 0= if  \ Return first match
    >dn.child @
    begin ?dup while
      dup dup " device_type" rot get-package-property 0= if
        drop dup cstrlen
        " keyboard" strcmp 0= if
          dup to keyboard-phandle
        then
      then
      (find-keyboard-device)
      >dn.peer @
    repeat
  else
    drop
  then
;

\ create the keyboard devalias
:noname
  device-tree @ (find-keyboard-device)
  keyboard-phandle @ if
    active-package
    " /aliases" find-device
    keyboard-phandle @ get-package-path 2dup
    encode-string " kbd" property
    encode-string " keyboard" property
    active-package!
  then
; SYSTEM-initializer

\ Fixup device tree for Wii platform
: fixup-device-tree-rvl ( -- )
  active-package

  \ DT root
  " /" find-device
  " NTDOY,Revolution" model
  " Wii" encode-string " NTDOY,Revolution" encode-string encode+ " compatible" property \ TODO: System Profiler takes the first string in compatible
  h# e7be2c0 encode-int " clock-frequency" property
  \ MEM2 buffers for USB, etc
  \ Needs to be in sync with memory provided to XNU and the memory region.
  h# 101C2000 encode-int 23e000 encode-int encode+ " mem2-addresses" property

  \ Platform interrupt controller MMIO
  " /interrupt-controller" find-device
  h# 0c003000 encode-int 100 encode-int encode+ " reg" property

  \ DSP MMIO
  " /dsp" find-device
  h# 0c005000 encode-int 200 encode-int encode+ " reg" property

  \ Create Hollywood interrupt controller
  " /" find-device
  new-device
    " interrupt-controller" device-name
    " interrupt-controller" device-type
    " NTDOY,hlwd-pic" model
    " NTDOY,hlwd-pic" encode-string " compatible" property
    " " encode-string " built-in" property
    " " encode-string " interrupt-controller" property
    1 encode-int " #interrupt-cells" property
    h# 0d800030 encode-int 10 encode-int encode+ " reg" property
    d# 14 encode-int " interrupts" property
    " /interrupt-controller@0c003000" find-dev if
      encode-int " interrupt-parent" property
    then
  finish-device

  \ Set Hollywood as interrupt controller parent on Wii hardware devices
  " /aes" find-device
  " /interrupt-controller@0d800030" find-dev if
    encode-int " interrupt-parent" property
  then
  " /sha" find-device
  " /interrupt-controller@0d800030" find-dev if
    encode-int " interrupt-parent" property
  then
  " /usb@0d040000" find-device
  " /interrupt-controller@0d800030" find-dev if
    encode-int " interrupt-parent" property
  then
  " /usb@0d050000" find-device
  " /interrupt-controller@0d800030" find-dev if
    encode-int " interrupt-parent" property
  then
  " /usb@0d060000" find-device
  " /interrupt-controller@0d800030" find-dev if
    encode-int " interrupt-parent" property
  then
  " /sdhc" find-device
  " /interrupt-controller@0d800030" find-dev if
    encode-int " interrupt-parent" property
  then
  " /sdio" find-device
  " /interrupt-controller@0d800030" find-dev if
    encode-int " interrupt-parent" property
  then
  " /ipc" find-device
  " /interrupt-controller@0d800030" find-dev if
    encode-int " interrupt-parent" property
  then

  \ Create GX-related devices
  \ Command processor
  " /" find-device
  new-device
    " command-processor" device-name
    " NTDOY,command-processor" model
    " NTDOY,command-processor" encode-string " compatible" property
    " " encode-string " built-in" property
    h# 0c000000 encode-int 80 encode-int encode+ " reg" property
    d# 11 encode-int " interrupts" property
    " /interrupt-controller@0c003000" find-dev if
      encode-int " interrupt-parent" property
    then
  finish-device

  \ Pixel engine
  " /" find-device
  new-device
    " pixel-engine" device-name
    " NTDOY,pixel-engine" model
    " NTDOY,pixel-engine" encode-string " compatible" property
    " " encode-string " built-in" property
    h# 0c001000 encode-int 100 encode-int encode+ " reg" property
    d# 9 encode-int d# 10 encode-int encode+ " interrupts" property
    " /interrupt-controller@0c003000" find-dev if
      encode-int " interrupt-parent" property
    then
  finish-device

  \ Video interface (used for Open Firmware output)
  \ MMIO1: registers
  \ MMIO2: FB memory (needs to sync with other references)
  \ MMIO3: XFB memory (needs to sync with other references)
  " /" find-device
  new-device
    " video" device-name
    " NTDOY,video" model
    " NTDOY,video" encode-string " compatible" property
    " " encode-string " built-in" property
    h# 0c002000 encode-int 100 encode-int encode+
      10000000 encode-int encode+ 12c000 encode-int encode+
      1012c000 encode-int encode+ 96000 encode-int encode+ " reg" property
    d# 8 encode-int " interrupts" property
    " /interrupt-controller@0c003000" find-dev if
      encode-int " interrupt-parent" property
    then
  finish-device

  \ FIFO
  " /" find-device
  new-device
    " fifo" device-name
    " NTDOY,fifo" model
    " NTDOY,fifo" encode-string " compatible" property
    " " encode-string " built-in" property
    h# 0c008000 encode-int 4 encode-int encode+ " reg" property
  finish-device

  active-package!
;

\ -------------------------------------------------------------------------
\ Mac OS 9 personality (Wii)
\ -------------------------------------------------------------------------
\
\ The Mac OS 9 Trampoline only accepts an OpenPIC or a Heathrow/Paddington
\ interrupt controller, and in Heathrow mode it does not follow cascades: it
\ expects one controller with a flat space of 64 sources. So the Flipper PIC
\ is presented as the Heathrow, and the Hollywood PIC behind its line 14 is
\ folded into the same space:
\
\    sources  0-31   Flipper PIC lines
\    sources 32-63   Hollywood PIC lines 0-31
\
\ The Trampoline finds the controller through the "interrupt-controller"
\ property of /chosen, which it uses as a package handle.

\ Active package: move from Hollywood line n to flat source 32+n.
: os9-flatten-hlwd-irq ( -- )
  " interrupts" active-package get-package-property 0= if
    decode-int nip nip d# 32 + encode-int " interrupts" property
  then
  " /interrupt-controller@0c003000" find-dev if
    encode-int " interrupt-parent" property
  then
;

: fixup-device-tree-os9 ( -- )
  active-package

  \ The Mac OS ROM boot script refuses machines that are not "MacRISC".
  " /" find-device
  " MacRISC" encode-string " MacRISC2" encode-string encode+
    " Power Macintosh" encode-string encode+ " compatible" property

  " /interrupt-controller@0c003000" find-device
  " heathrow" encode-string " NTDOY,pic" encode-string encode+ " compatible" property

  \ No longer a controller as far as the tree is concerned; it stays as the
  \ device that owns Flipper line 14.
  " /interrupt-controller@0d800030" find-device
  " interrupt-controller" delete-property

  " /aes" find-device os9-flatten-hlwd-irq
  " /sha" find-device os9-flatten-hlwd-irq
  " /usb@0d040000" find-device os9-flatten-hlwd-irq
  " /usb@0d050000" find-device os9-flatten-hlwd-irq
  " /usb@0d060000" find-device os9-flatten-hlwd-irq
  " /sdhc" find-device os9-flatten-hlwd-irq
  " /sdio" find-device os9-flatten-hlwd-irq
  " /ipc" find-device os9-flatten-hlwd-irq

  " /chosen" find-device
  " /interrupt-controller@0c003000" find-dev if
    encode-int " interrupt-controller" property
  then

  \ Mac OS 9 will not start without a VIA and a power manager plug-in, and
  \ the Wii has neither. Describe a VIA with a Cuda behind it, as on a Blue &
  \ White G3 (which also goes with the Heathrow-style interrupt controller
  \ above), so the Trampoline loads the Cuda power plug-in. That plug-in is
  \ far less demanding than the Core99 PMU one, which expects KeyLargo,
  \ UniNorth and thermostats around it.
  \
  \ The VIA is emulated by the Mac OS kernel add-on. It sits at 16000 in the
  \ emulated device region, where a Heathrow has its VIA (see the mac-io
  \ node below), and claims line 15, which no Wii device can raise.
  " /" find-device
  new-device
    " via-cuda" device-name
    " via-cuda" device-type
    " cuda" encode-string " compatible" property
    " " encode-string " built-in" property
    1 encode-int " #address-cells" property
    0 encode-int " #size-cells" property
    h# de016000 encode-int h# 2000 encode-int encode+ " reg" property
    \ Mac OS drivers take the VIA's address from here. The Trampoline only
    \ creates this property for children of a mac-io node.
    h# de016000 encode-int " AAPL,address" property
    d# 15 encode-int " interrupts" property
    " /interrupt-controller@0c003000" find-dev if
      encode-int " interrupt-parent" property
    then

    \ The ADB bus behind the Cuda. Without this node the Trampoline assumes
    \ USB-emulated ADB and the ROM installs its Egret flavour of the chip
    \ manager, which drives the handshake lines the other way up.
    new-device
      " adb" device-name
      " adb" device-type
      " adb" encode-string " compatible" property
      1 encode-int " #address-cells" property
      0 encode-int " #size-cells" property
    finish-device

    new-device
      " rtc" device-name
      " rtc" device-type
      " rtc" encode-string " compatible" property
    finish-device

    new-device
      " power-mgt" device-name
      " power-mgt" device-type
      " min-consumption-pwm-led" encode-string " mgt-kind" property
      " cuda" encode-string " compatible" property
    finish-device
  finish-device

  \ An ATA bus of the kind a Heathrow carries, with one disk. Mac OS drives
  \ it with its own ATA Manager and disk driver; the registers are emulated
  \ by the Mac OS kernel add-on, which gets the sectors from wherever the
  \ machine really has storage. Sources 16 and 17 are the bus and its (unused)
  \ DMA channel; no Wii device can raise them.
  \
  \ Apple's ATA driver refuses to start unless an ancestor of the bus has
  \ device type "mac-io" and an "AAPL,address", so the bus sits under a
  \ mac-io node that stands for the whole emulated device region. The
  \ Trampoline takes a mac-io's base address from its second "reg" entry.
  \
  \ Offsets inside the region are a Heathrow's (interrupt controller at 10,
  \ DBDMA channels at 8000, VIA at 16000, ATA at 20000). Some Mac OS code
  \ takes them for granted instead of reading this tree: with the VIA at 0,
  \ a write Mac OS makes to the start of mac-io while it enables the ATA
  \ interrupt landed on the VIA's port B and broke the Cuda handshake.
  " /" find-device
  new-device
    " mac-io" device-name
    " mac-io" device-type
    " " encode-string " built-in" property
    1 encode-int " #address-cells" property
    1 encode-int " #size-cells" property
    h# de000000 encode-int h# 01000000 encode-int encode+
      h# de000000 encode-int encode+ h# 01000000 encode-int encode+ " reg" property
    h# de000000 encode-int " AAPL,address" property

    new-device
      " ata-0" device-name
      " ata" device-type
      " heathrow-ata" encode-string " compatible" property
      " ata-4" model
      " ata" encode-string " AAPL,connector" property
      " " encode-string " built-in" property
      1 encode-int " #address-cells" property
      0 encode-int " #size-cells" property
      h# 20000 encode-int h# 1000 encode-int encode+
        h# 8b00 encode-int encode+ h# 100 encode-int encode+ " reg" property
      h# de020000 encode-int h# de008b00 encode-int encode+ " AAPL,address" property
      0 encode-int " AAPL,bus-id" property
      h# 526 encode-int h# 85 encode-int encode+ h# 25 encode-int encode+
        h# 25 encode-int encode+ h# 25 encode-int encode+ 0 encode-int encode+
        0 encode-int encode+ 0 encode-int encode+ " AAPL,pio-timing" property
      d# 16 encode-int d# 17 encode-int encode+ " interrupts" property
      " /interrupt-controller@0c003000" find-dev if
        encode-int " interrupt-parent" property
      then
    finish-device
  finish-device

  \ Mac OS gets a Heathrow in the emulated device region (see wii.h): its
  \ registers are answered by the Mac OS kernel add-on, not by the Flipper
  \ PIC. Done last: the lookups above find the node by its physical unit
  \ address.
  " /interrupt-controller@0c003000" find-device
  h# de000010 encode-int h# 20 encode-int encode+ " reg" property

  active-package!
;

\ Fixup device tree for Wii U platform
: fixup-device-tree-cafe ( -- )
  active-package

  \ DT root
  " /" find-device
  " NTDOY,Cafe" model
  " WiiU" encode-string " NTDOY,Cafe" encode-string encode+ " compatible" property \ TODO: System Profiler takes the first string in compatible
  h# ed1b768 encode-int " clock-frequency" property

  \ Platform interrupt controller MMIO
  " /interrupt-controller" find-device
  h# 0c000000 encode-int 100 encode-int encode+ " reg" property

  \ DSP MMIO
  " /dsp" find-device
  h# 0c280000 encode-int 200 encode-int encode+ " reg" property

  \ Create Latte interrupt controller
  " /" find-device
  new-device
    " interrupt-controller" device-name
    " interrupt-controller" device-type
    " NTDOY,latte-pic" model
    " NTDOY,latte-pic" encode-string " compatible" property
    " " encode-string " built-in" property
    " " encode-string " interrupt-controller" property
    1 encode-int " #interrupt-cells" property
    h# 0d800440 encode-int 48 encode-int encode+ " reg" property
    d# 24 encode-int " interrupts" property
    " /interrupt-controller@0c000000" find-dev if
      encode-int " interrupt-parent" property
    then
  finish-device

  \ Set Latte as interrupt controller parent on Wii hardware devices
  " /aes" find-device
  " /interrupt-controller@0d800440" find-dev if
    encode-int " interrupt-parent" property
  then
  " /sha" find-device
  " /interrupt-controller@0d800440" find-dev if
    encode-int " interrupt-parent" property
  then
  " /usb@0d040000" find-device
  " /interrupt-controller@0d800440" find-dev if
    encode-int " interrupt-parent" property
  then
  " /usb@0d050000" find-device
  " /interrupt-controller@0d800440" find-dev if
    encode-int " interrupt-parent" property
  then
  " /usb@0d060000" find-device
  " /interrupt-controller@0d800440" find-dev if
    encode-int " interrupt-parent" property
  then
  " /sdhc" find-device
  " /interrupt-controller@0d800440" find-dev if
    encode-int " interrupt-parent" property
  then
  " /sdio" find-device
  " /interrupt-controller@0d800440" find-dev if
    encode-int " interrupt-parent" property
  then
  " /ipc" find-device
  " /interrupt-controller@0d800440" find-dev if
    encode-int " interrupt-parent" property
  then

  \ Create GX2 GPU stub device
  \ Additional properties will be added in the GX2 driver.
  " /" find-device
  new-device
    " gx2" device-name
    " NTDOY,gx2" model
    " NTDOY,gx2" encode-string " compatible" property
    " " encode-string " built-in" property
    d# 2 encode-int " interrupts" property
    " /interrupt-controller@0c000000" find-dev if
      encode-int " interrupt-parent" property
    then
  finish-device

  \ OHCI USB controller 1 (front ports)
  " /" find-device
  new-device
    " usb" device-name
    " usb" device-type
    " NTDOY,ohci" model
    " NTDOY,ohci" encode-string " compatible" property
    " " encode-string " built-in" property
    h# 0d130000 encode-int 100 encode-int encode+ " reg" property
    d# 35 encode-int " interrupts" property
    " /interrupt-controller@0d800440" find-dev if
      encode-int " interrupt-parent" property
    then
  finish-device

  \ OHCI USB controller 2 (GamePad)
  " /" find-device
  new-device
    " usb" device-name
    " usb" device-type
    " NTDOY,ohciBT" model
    " NTDOY,ohciBT" encode-string " compatible" property
    " " encode-string " built-in" property
    h# 0d150000 encode-int 100 encode-int encode+ " reg" property
    d# 37 encode-int " interrupts" property
    " /interrupt-controller@0d800440" find-dev if
      encode-int " interrupt-parent" property
    then
  finish-device

  active-package!
;

\ -------------------------------------------------------------------------
\ pre-booting
\ -------------------------------------------------------------------------

: update-chosen
  " /chosen" find-device
  stdin @ encode-int " stdin" property
  stdout @ encode-int " stdout" property
  device-end
;

:noname
  set-defaults
; PREPOST-initializer

\ -------------------------------------------------------------------------
\ Mac OF specific words
\ -------------------------------------------------------------------------

: parse-1hex 1 parse-nhex ;
: parse-2hex 2 parse-nhex ;
: parse-3hex 3 parse-nhex ;

\ -------------------------------------------------------------------------
\ copyright property handling
\ -------------------------------------------------------------------------

: insert-copyright-property
  \ As required for MacOS 9 and below
  " Pbclevtug 1983-2001 Nccyr Pbzchgre, Vap. GUVF ZRFFNTR SBE PBZCNGVOVYVGL BAYL"
  rot13-str encode-string " copyright"
  " /" find-package if
    " set-property" $find if
      execute
    else
      3drop drop
    then
  then
;

: delete-copyright-property
  \ Remove copyright property created above
  active-package
  " /" find-package if
      active-package!
      " copyright" delete-property
  then
  active-package!
;

: (exit)
  \ Clean up before returning to the interpreter
  delete-copyright-property
;

\ -------------------------------------------------------------------------
\ Adler-32 wrapper
\ -------------------------------------------------------------------------

: adler32 ( adler buf len -- checksum )
  " (adler32)" $find if
    execute
  else
    ." Can't find " ( adler32-name ) type cr
    3drop 0
  then
;

[IFDEF] CONFIG_DRIVER_FLIPPER_VI
  -1 value flipper-vi-driver-fcode
  " NTDOY,video.bin" $encode-file to flipper-vi-driver-fcode
[THEN]
[IFDEF] CONFIG_DRIVER_WII_GX2
  -1 value wii-gx2-driver-fcode
  " Wii,GX2.bin" $encode-file to wii-gx2-driver-fcode
[THEN]
