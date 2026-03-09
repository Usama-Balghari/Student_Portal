$(document).ready(function () {

        $("#tblViewProfile").on("click", ".delete-btn", function () {

        var button = $(this);
        var userId = button.data("id");
        var row = button.closest("tr");

        Swal.fire({
            title: 'Are you sure?',
        text: "User will be permanently deleted!",
        icon: 'warning',
        showCancelButton: true,
        confirmButtonColor: '#d33',
        cancelButtonColor: '#6c757d',
        confirmButtonText: 'Yes, delete'
        }).then((result) => {

            if (result.isConfirmed) {

            $.ajax({
                url: '/Account/DeleteUser',
                type: 'POST',
                data: { userId: userId },

                success: function (response) {

                    if (response.success) {

                        row.fadeOut(400, function () {
                            $(this).remove();
                        });

                        Swal.fire(
                            'Deleted!',
                            'User has been deleted.',
                            'success'
                        );
                    }
                    else {
                        Swal.fire(
                            'Error',
                            response.message,
                            'error'
                        );
                    }
                }
            });

            }

        });

    });


    //EditStudent

    //$(".edit-btn").click(function (){
    $('#tblStudents').on('click', '.edit-btn', function () { 
        var id = $(this).data("id");

        $.ajax({
            url: "/Students/Edit/" + id,
            type: "GET",
            success: function (response) {
                $("#editModalContent").html(response);
                $.validator.unobtrusive.parse("#editForm");
                $("#EditModal").modal("show");
            }       
        });
    });

    //End StudentPart

    //document.getElementById("ViewProfile").onclick = function ()
    $('#tblViewProfile').on('click', '.view-profile-btn', function (e) {
        e.preventDefault();
        var id = $(this).data("id");
        var url = $(this).attr("href");

        $.ajax({
            url: url,
            type: "GET",
            success: function (response) {
                $("#editModalContent").html(response);
                //$.validator.unobtrusive.parse("#editForm");
                $("#ViewProfileModal").modal("show");
            },
            error: function () {
                alert("Error loading profile.");
            }
        });
    });
    $(document).on('submit', 'form[asp-action="ToggleAdmin"]', function (e) {
        e.preventDefault(); // Stop the form from reloading the page

        var $form = $(this);
        var url = $form.attr('action');
        var formData = $form.serialize(); // Grabs userId and makeAdmin from hidden inputs

        $.post(url, formData, function (response) {
            // Option 1: Reload just the table part
            // Option 2: Show a success message
            alert("Status updated successfully!");
            location.reload(); // Quickest way to see the badge change
        }).fail(function () {
            alert("Something went wrong!");
        });
    });





    //CoursePart Start from here...





    $('#tblCourses').on('click', '.delete-course-btn', function () {

        let id = $(this).data("id");
        if (!confirm("Are you sure you want to delete this?")) {
            return;
        }
        $.ajax({
            url: '/Courses/DeleteCourse',
            type: 'POST',
            data: { id: id },
            //headers: {
            //    "RequestVerificationToken": $('input[name="__RequestVerificationToken"]').val()
            //},
            success: function (response) {
                if (!response.success) {
                    $("#success").text(response.message);
                    $("#success-alert").fadeIn();

                    setTimeout(function () {
                        $("#success-alert").fadeOut();
                    }, 4000);
                }
                if (response.success) {

                    $('#row-' + id).remove();

                    $("#success").text(response.message);
                    $("#success-alert").fadeIn();

                    setTimeout(function () {
                        $("#success-alert").fadeOut();
                    }, 4000);
                }
            }
        });
    });












        //EditCourse
    $('#tblCourses').on('click', '.edit-course-btn', function () {
        var id = $(this).data("id");

        $.ajax({
            url: "/Courses/Edit/" + id,
            type: "GET",
            success: function (response) {
                $("#editModalContent").html(response);
                $.validator.unobtrusive.parse("#editDeptForm");
                $("#EditCourseModal").modal("show");
            }
        });
    });
    //Enrolled CoursesPart
    $(document).on("click", ".enrolled-course-btn", function (e) {
        e.preventDefault();
        var id = $(this).data("id");

        if (!id) return; // Guard against empty IDs

        $.get("/Students/GetEnrolledCourses", { id: id }, function (res) {

            let html = "";
            res.courses.forEach(c => {
                html += `<li>${c.courseName}</li>`;
            });

            $("#courseList").html(html || "<li>No courses enrolled</li>");

            // Use Bootstrap 5 syntax for the modal
            var modal = new bootstrap.Modal(document.getElementById('enrolledCourseModal'));
            modal.show();
        });
    });

    let removedCourses = [];

    $(document).on("click", ".remove-course", function () {
        let courseId = $(this).data("course-id");

        removedCourses.push(courseId);
        $("#removedCourseIds").val(removedCourses.join(","));

        $(this).closest("li").remove();
    });
    //End CoursesPart


    // DepartmentPart Start from Here to...
        //EditDepartment
    $('#tblDepartments').on('click', '.edit-dept-btn', function () {
        var id = $(this).data("id");

        $.ajax({
            url: "/Departments/Edit/" + id,
            type: "GET",
            success: function (response) {
                $("#editModalContent").html(response);
                $.validator.unobtrusive.parse("#editDeptForm");
                $("#EditDeptModal").modal("show");
            }
        });
    });


    //DeleteDepartment

    $('#tblDepartments').on('click', '.delete-dept-btn', function () {
        
        let id = $(this).data("id");
        if (!confirm("Are you sure you want to delete this?")) {
            return;
        }
        $.ajax({
            url: '/Departments/DeleteConfirmed',
            type: 'POST',
            data: { id: id },
            headers: {
                "RequestVerificationToken": $('input[name="__RequestVerificationToken"]').val()
            },
            success: function (response) {
                if (!response.success) {
                    $("#success").text(response.message);
                    $("#success-alert").fadeIn();

                    setTimeout(function () {
                        $("#success-alert").fadeOut();
                    }, 4000);
                }
                if (response.success) {

                    $('#row-' + id).remove(); 

                    $("#success").text(response.message);
                    $("#success-alert").fadeIn();

                    setTimeout(function () {
                        $("#success-alert").fadeOut();
                    }, 4000);
                }
            }
        });
    });

    //End DepartmentPart

    var msg = document.getElementById("msg");
    setTimeout(function () {
        $(msg).fadeOut();
    }, 4000);


});


