﻿//<script language="JavaScript">
//<!--
//
function Init() {

    $("#TXTIDA").change(function () {
        let controlActorId = $(this).prop('id');
        let idA = GetIdAutocompleteInput(controlActorId);
        
        $("#DDLIDB_INVOICING").empty();
        if (!isNaN(parseInt(idA)) && parseInt(idA) > 0) {
            LoadDataTable(['IDB', 'column:IDENTIFIER,columnDisplay:DISPLAYNAME'], 'VW_BOOK_VIEWER', [{ col: 'IDA', value: parseInt(idA) }], function (b) {
                LoadDDL('DDLIDB_INVOICING', b, true);
            });
        }
    });

}
// -->
//</script>