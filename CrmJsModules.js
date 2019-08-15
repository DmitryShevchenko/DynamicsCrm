// Show and hide tab And section
function showHideTabAndSectionInLead() {
    var contactType = Xrm.Page.getAttribute("ds_leadtype").getValue();
    if (contactType == 717590000) {
        Xrm.Page.ui.tabs.get("Summary").sections.get("Coach").setVisible(true);
        Xrm.Page.ui.tabs.get("University_Data").setVisible(true);
        Xrm.Page.ui.tabs.get("know_Info").setVisible(true);
    } else {
        Xrm.Page.ui.tabs.get("Summary").sections.get("Coach").setVisible(false);
        Xrm.Page.ui.tabs.get("University_Data").setVisible(false);
        Xrm.Page.ui.tabs.get("know_Info").setVisible(false);
    }
    if (contactType == 717590001) {
        Xrm.Page.ui.tabs.get("Summary").sections.get("Account").setVisible(true);
        Xrm.Page.ui.tabs.get("details_tab").setVisible(true);
    } else {
        Xrm.Page.ui.tabs.get("Summary").sections.get("Account").setVisible(false);
        Xrm.Page.ui.tabs.get("details_tab").setVisible(false);
    }
}

function showHideTabAndSectionInContact() {
    var contactType = Xrm.Page.getAttribute("ds_contacttype").getValue();
    if (contactType == 717590000) {
        Xrm.Page.ui.tabs.get("University_Data").setVisible(true);
        Xrm.Page.ui.tabs.get("Service_User_Activities").setVisible(true);
        Xrm.Page.ui.tabs.get("urstab").setVisible(false);
    } else {
        Xrm.Page.ui.tabs.get("University_Data").setVisible(false);
        Xrm.Page.ui.tabs.get("Service_User_Activities").setVisible(false);
        Xrm.Page.ui.tabs.get("urstab").setVisible(true);
    }
    if (contactType == 717590001) {
        Xrm.Page.ui.tabs.get("SUMMARY_TAB").sections.get("SUMMARY_TAB8").setVisible(true);
        Xrm.Page.ui.tabs.get("urstab").setVisible(true);
    } else {
        Xrm.Page.ui.tabs.get("SUMMARY_TAB").sections.get("SUMMARY_TAB8").setVisible(false);
        Xrm.Page.ui.tabs.get("urstab").setVisible(false);
    }
}

// DocumentFrame
function SetDocumentFrame() {
    var url = Xrm.Page.context.getClientUrl() +
        "/userdefined/areas.aspx?formid=ab44efca-df12-432e-a74a-83de61c3f3e9&inlineEdit=1&navItemName=Documents&oId=%7b" +
        Xrm.Page.data.entity.getId().replace("{", "").replace("}", "") + "%7d&oType=" +
        Xrm.Page.context.getQueryStringParameters().etc +
        "&pagemode=iframe&rof=true&security=852023&tabSet=areaSPDocuments&theme=Outlook15White";

    Xrm.Page.getControl("IFRAME_SharePoint").setSrc(url);
}

//Assosiate
function Owner() {
    var userSettings = Xrm.Utility.getGlobalContext().userSettings;
    Xrm.Page.getAttribute("ownerid").setValue([{ id: userSettings.userId, name: userSettings.userName, entityType: "systemuser" }])
    Xrm.Page.data.save();
}

// Lead converting
function convertToLead() {
    debugger;
    let lead = Xrm.Page.getAttribute("ds_leadid").getValue();

    if (lead == null) {
        let recordId = Xrm.Page.data.entity.getId().replace(/[{}]/gi, "");
        Xrm.WebApi.retrieveRecord("ds_studentrequest", recordId, "?$select=ds_firstname,ds_lastname,ds_mobilephone,emailaddress,_ds_universitygroupid_value,ds_otherspecialityinfo").then(
            function success(result) {
                let data = {
                    "firstname": result.ds_firstname,
                    "lastname": result.ds_lastname,
                    "mobilephone": result.ds_mobilephone,
                    "emailaddress1": result.emailaddress,
                    "ds_UniversityGroupId@odata.bind": "/ds_universitygroups(" + result._ds_universitygroupid_value + ")",
                    "ds_otherinfo": result.ds_otherspecialityinfo,
                };
                Xrm.WebApi.createRecord("lead", data).then(
                    function success(result) {
                        var entityFormOptions = {
                            "entityName": "lead",
                            "entityId": result.id
                        }
                        //error
                        // In data, property key should be (field name ds_leadid) uppercase like ds_Leadid. (For custom entities) shemaName
                        // This is not indicated in the documentation.
                        Xrm.WebApi.updateRecord("ds_studentrequest", recordId, { "ds_Leadid@odata.bind": "/leads(" + result.id + ")", "statecode": 1, }).then(
                            function success(result) {

                                Xrm.Navigation.openForm(entityFormOptions);
                            }
                        );
                    }
                );
            }
        );
    } else {
        let leadid = Xrm.Page.getAttribute("ds_leadid").getValue()[0].id.replace(/[{}]/gi, "");
        Xrm.WebApi.retrieveRecord("lead", leadid, "?$select=statecode,statuscode").then(
            function success(result) {
                let data = {
                    "ownerid@odata.bind": "/systemusers(" + Xrm.Page.context.getUserId().replace("{", "").replace("}", "") + ")",
                    "statecode": 0,
                    "statuscode": 1,
                }
                if (result.statecode == 2 && result.statuscode == 5) {
                    Xrm.WebApi.updateRecord("lead", leadid, data).then(
                        function success() {
                            Xrm.Navigation.openForm({ "entityName": "lead", "entityId": leadid });
                        }
                    );
                }
            }
        )
    }
}



// Lead Data update if Groupid Change
function LeadGroupChange() {
    let groupId = Xrm.Page.getAttribute("ds_universitygroupid").getValue();

    if (groupId != null) {
        Xrm.WebApi.retrieveRecord("ds_universitygroup", groupId[0].id.replace(/[{}]/gi, ""), "?$select=_ds_universityspecialityid_value").then(
            function success(r1) {
                Xrm.WebApi.retrieveRecord("ds_universityspeciality", r1._ds_universityspecialityid_value.replace(/[{}]/gi, ""), "?$select=_ds_universitychairid_value").then(
                    function success(r2) {
                        let data = {
                            "ds_uSpecialityId@odata.bind": "/ds_universityspecialities(" + r1._ds_universityspecialityid_value.replace(/[{}]/gi, "") + ")",
                            "ds_uChairId@odata.bind": "/ds_universitychairs(" + r2._ds_universitychairid_value.replace(/[{}]/gi, "") + ")",

                        }
                        Xrm.WebApi.updateRecord("lead", Xrm.Page.data.entity.getId().replace(/[{}]/gi, ""), data).then(
                            function (result) {
                                Xrm.Page.data.refresh(true);
                            }
                        );
                    });
            });
    } else {
        Xrm.Page.getAttribute("ds_uchairid").setValue(null);
        Xrm.Page.getAttribute("ds_uspecialityid").setValue(null);
    }

}



//PreFiltering

function preFilterChecklistTypeLookup() {
    debugger;
    var accountID = Xrm.Page.getAttribute("ds_accountid").getValue();

    if (accountID != null) {

        var lookupControl = Xrm.Page.getControl("ds_vacancyid");

        if (lookupControl != null) {

            lookupControl.addPreSearch(addLookupFilter);
            lookupControl.setDisabled(false);
        }
    }
}

function preFilterChecklistTypeLookupVacancy() {
    debugger;
    var vacancyID = Xrm.Page.getAttribute("ds_vacancyid").getValue();

    if (vacancyID != null) {

        var account = Xrm.Page.getAttribute("ds_accountid");

        if (account != null) {
            Xrm.WebApi.retrieveRecord("account", vacancyID[0].id, "?$select=ds_companyname").then(function success(result) {
                account.SetValue([{ id: result.id, name: result.name, entityType: result.entityName }]);
            },
                function (error) {
                    console.log(error.message);
                }
            );
        }
    }
}


//UNIVERSITY EDUCATION PROGRAM MANAGER

function NamingUEPM() {
    let subject = Xrm.Page.getAttribute("ds_subjectid").getValue();
    let universitygroup = Xrm.Page.getAttribute("ds_universitygroupid").getValue();


    if (subject != null && universitygroup != null) {
        Xrm.Page.getAttribute("ds_name").setValue(subject[0].name + "    " + "UniversityGroup_id: " + universitygroup[0].id);
        Xrm.Page.data.entity.save();
    } else {
        Xrm.Page.getAttribute("ds_name").setValue("undefined");        
    }
}


//TestQuestion

function FilterTestQuestion() {
    let lookupControl = Xrm.Page.getControl("ds_theme");
    if (lookupControl != null) {
        lookupControl.setDisabled(true);
    }
}

function addLookupFilterTestQuestion() {
    Xrm.Page.getAttribute("ds_theme").setValue(null);
    var subjectId = Xrm.Page.getAttribute("ds_subjectid").getValue();

    var lookupControl = Xrm.Page.getControl("ds_theme");
    if (lookupControl != null) {

        lookupControl.setDisabled(true);
        if (subjectId != null) {

            lookupControl.addPreSearch(function addLookupFilter() {
                var subjectId = Xrm.Page.getAttribute("ds_subjectid").getValue()[0].id.replace(/[{}]/gi, "");
                if (subjectId != null) {
                    var fetchXML = "<filter type = 'and'><condition attribute='ds_subjectid' operator='eq' value='" + subjectId + "'/></filter>";
                    Xrm.Page.getControl("ds_theme").addCustomFilter(fetchXML);
                }
            });
            lookupControl.setDisabled(false);
        }
    }
}

//VacancyPositionExpentant

function FilterVacancyPositionExpentantQQ() {
    let lookupControl = Xrm.Page.getControl("ds_vacancyid");
    let lookupControl2 = Xrm.Page.getControl("ds_accountid")
    let lookupControl3 = Xrm.Page.getControl("ds_technicaltaskmanagerid")
    if (lookupControl != null && lookupControl2 != null && lookupControl3 != null) {
        lookupControl.setDisabled(true);
        lookupControl2.setDisabled(true);
        lookupControl3.setDisabled(true);
    }
}

function FilterVacancyPositionExpentantContactQQ() {

    debugger;
    let contactId = Xrm.Page.getAttribute("ds_contactid").getValue();
    
    let lookupControl = Xrm.Page.getControl("ds_technicaltaskmanagerid");
    
     if (contactId == null) {
        if (lookupControl != null) {            
            Xrm.Page.getAttribute("ds_technicaltaskmanagerid").setValue(null);
            lookupControl.setDisabled(true);
            
        }
    } else {
        if (lookupControl != null) {            
            Xrm.Page.getAttribute("ds_technicaltaskmanagerid").setValue(null);
            lookupControl.setDisabled(false);            
        }
    }
}


function FilterVacancyPositionExpentant() {
    let lookupControl = Xrm.Page.getControl("ds_vacancyid");
    let lookupControl2 = Xrm.Page.getControl("ds_technicaltaskmanagerid")
    if (lookupControl != null && lookupControl2 != null) {
        lookupControl.setDisabled(true);
        lookupControl2.setDisabled(true);
    }
}
// contact on change

function FilterVacancyPositionExpentantContact() {

    debugger;
    let contactId = Xrm.Page.getAttribute("ds_contactid").getValue();
    let companyId = Xrm.Page.getAttribute("ds_accountid").getValue();

    let lookupControl = Xrm.Page.getControl("ds_vacancyid");
    let lookupControl2 = Xrm.Page.getControl("ds_technicaltaskmanagerid");

    if (contactId == null && companyId == null) {
        if (lookupControl != null && lookupControl2 != null) {
            Xrm.Page.getAttribute("ds_vacancyid").setValue(null);
            Xrm.Page.getAttribute("ds_technicaltaskmanagerid").setValue(null);
            lookupControl.setDisabled(true);
            lookupControl2.setDisabled(true);
        }
    } else if (contactId == null && companyId != null) {
        if (lookupControl != null && lookupControl2 != null) {
            Xrm.Page.getAttribute("ds_vacancyid").setValue(null);
            Xrm.Page.getAttribute("ds_technicaltaskmanagerid").setValue(null);
            lookupControl2.setDisabled(true);
        }
    } else if (contactId != null && companyId == null) { 
        if (lookupControl != null && lookupControl2 != null) {
            Xrm.Page.getAttribute("ds_vacancyid").setValue(null);
            Xrm.Page.getAttribute("ds_technicaltaskmanagerid").setValue(null);
            lookupControl.setDisabled(true);
            lookupControl2.setDisabled(true);
        }
    }
    else if (contactId == null) {
        if (lookupControl != null && lookupControl2 != null) {
            Xrm.Page.getAttribute("ds_vacancyid").setValue(null);
            Xrm.Page.getAttribute("ds_technicaltaskmanagerid").setValue(null);
            lookupControl.setDisabled(true);
            lookupControl2.setDisabled(true);
        }
    } else {
        if (lookupControl != null && lookupControl2 != null) {
            Xrm.Page.getAttribute("ds_vacancyid").setValue(null);
            Xrm.Page.getAttribute("ds_technicaltaskmanagerid").setValue(null);
            lookupControl.setDisabled(false);
            lookupControl2.setDisabled(false);
        }
    }
}



function addLookupFilterVacancyPositionExpentantVacancy() {
    debugger;
    Xrm.Page.getAttribute("ds_technicaltaskmanagerid").setValue(null);
    Xrm.Page.getAttribute("ds_vacancyid").setValue(null);
    let companyId = Xrm.Page.getAttribute("ds_accountid").getValue();

    let lookupControl = Xrm.Page.getControl("ds_vacancyid");
    let lookupControl2 = Xrm.Page.getControl("ds_technicaltaskmanagerid")
    if (lookupControl != null && lookupControl2 != null) {
        lookupControl.setDisabled(true);
        lookupControl2.setDisabled(true);
        if (companyId != null) {
            lookupControl.addPreSearch(function addLookupFilter() {
                var companyId = Xrm.Page.getAttribute("ds_accountid").getValue()[0].id.replace(/[{}]/gi, "");
                if (companyId != null) {
                    var fetchXML = "<filter type = 'and'><condition attribute='ds_companyname' operator='eq' value='" + companyId + "'/></filter>";
                    Xrm.Page.getControl("ds_vacancyid").addCustomFilter(fetchXML);
                }
            });
            lookupControl.setDisabled(false);
        }
    }
}

function addLookupFilterVacancyPositionExpentantTT() {
    debugger;
    Xrm.Page.getAttribute("ds_technicaltaskmanagerid").setValue(null);
    let vacancyId = Xrm.Page.getAttribute("ds_vacancyid").getValue();
    let contactId = Xrm.Page.getAttribute("ds_contactid").getValue();

    let lookupControl = Xrm.Page.getControl("ds_technicaltaskmanagerid");

    if (lookupControl != null) {
        lookupControl.setDisabled(true);
        if (vacancyId != null && contactId != null) {

            lookupControl.addPreSearch(function addLookupFilter() {                
                if (vacancyId != null) {
                    var fetchXML = "<filter type = 'and'><condition attribute='ds_vacancyid' operator='eq' value='" + vacancyId[0].id.replace(/[{}]/gi, "") + "'/><condition attribute='ds_contactid' operator='eq' value='" + contactId[0].id.replace(/[{}]/gi, "") + "'/></filter>";
                    Xrm.Page.getControl("ds_technicaltaskmanagerid").addCustomFilter(fetchXML);
                }
            });
            lookupControl.setDisabled(false);

        }
    }
}

//Technical Task Manager
function preFilterChecklistTypeLookup() {
    debugger;
    var accountID = Xrm.Page.getAttribute("ds_accountid").getValue();

    if (accountID != null) {

        var lookupControl = Xrm.Page.getControl("ds_vacancyid");

        if (lookupControl != null) {

            lookupControl.addPreSearch(addLookupFilter);
            lookupControl.setDisabled(false);
        }
    }
}

function addLookupFilter() {
    var accountID = Xrm.Page.getAttribute("ds_accountid").getValue();
    if (accountID != null) {
        var fetchXML = "<filter type = 'and'><condition attribute='ds_companyname' operator='eq' value='" + accountID[0].id + "'/></filter>";
        Xrm.Page.getControl("ds_vacancyid").addCustomFilter(fetchXML);
    }
}

//Technical Task Manager

function FilterTechnicalTaskManager() {
    let lookupControl = Xrm.Page.getControl("ds_vacancyid");
    let lookupControl2 = Xrm.Page.getControl("ds_technicaltaskid")
    if (lookupControl != null && lookupControl2 != null) {
        lookupControl.setDisabled(true);
        lookupControl2.setDisabled(true);
    }
}

function addLookupFilterTechnicalTaskManagerVacancy() {
    debugger;
    Xrm.Page.getAttribute("ds_technicaltaskid").setValue(null);
    Xrm.Page.getAttribute("ds_vacancyid").setValue(null);
    let companyId = Xrm.Page.getAttribute("ds_accountid").getValue();

    let lookupControl = Xrm.Page.getControl("ds_vacancyid");
    let lookupControl2 = Xrm.Page.getControl("ds_technicaltaskid")
    if (lookupControl != null && lookupControl2 != null) {
        lookupControl.setDisabled(true);
        lookupControl2.setDisabled(true);
        if (companyId != null) {
            lookupControl.addPreSearch(function addLookupFilter() {                
                if (companyId != null) {
                    var fetchXML = "<filter type = 'and'><condition attribute='ds_companyname' operator='eq' value='" + companyId[0].id.replace(/[{}]/gi, "") + "'/></filter>";
                    Xrm.Page.getControl("ds_vacancyid").addCustomFilter(fetchXML);
                }
            });
            lookupControl.setDisabled(false);
        }
    }
}

function addLookupFilterTechnicalTaskManagerTT() {
    debugger;
    Xrm.Page.getAttribute("ds_technicaltaskid").setValue(null);
    let vacancyId = Xrm.Page.getAttribute("ds_vacancyid").getValue();    

    let lookupControl = Xrm.Page.getControl("ds_technicaltaskid");

    if (lookupControl != null) {
        lookupControl.setDisabled(true);
        if (vacancyId != null ) {

            lookupControl.addPreSearch(function addLookupFilter() {                
                if (vacancyId != null) {
                    var fetchXML = "<filter type = 'and'><condition attribute='ds_vacancyid' operator='eq' value='" + vacancyId[0].id.replace(/[{}]/gi, "") + "'/></filter>";
                    Xrm.Page.getControl("ds_technicaltaskid").addCustomFilter(fetchXML);
                }
            });
            lookupControl.setDisabled(false);

        }
    }
}

function ValidateThePhoneNumber(executionContext){
    var formContext = typeof executionContext != 'undefined' ? executionContext.getFormContext() : Xrm.Page; 
    var fieldName = "sb_phone";
    var phoneData =  Xrm.Page.getAttribute(fieldName);
    var regExpPattern = /^\+?3?8?\s[(](0(66|95|99|50|69)[)]\s\d{3}\s\d{2}\s\d{2})$/gm;

    var result = RegExp(regExpPattern).test(phoneData.getValue());

    if (!result) {
        formContext.getControl(fieldName).clearNotification();
    } else {
        formContext.getControl(fieldName).setNotification('Invalid Value, phone number should be in format (+38 (XXX) XXX XX XX).');
    }
}






